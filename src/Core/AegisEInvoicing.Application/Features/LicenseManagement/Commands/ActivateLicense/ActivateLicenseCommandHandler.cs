using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Commands.ActivateLicense;

/// <summary>
/// Handler for activating a license key
/// Validates the key with licensing service and assigns to business
/// </summary>
public class ActivateLicenseCommandHandler(
    IApplicationDbContext context,
    ILicensingService licensingService,
    ICurrentUserService currentUserService,
    ILogger<ActivateLicenseCommandHandler> logger) : IRequestHandler<ActivateLicenseCommand, ActivateLicenseResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILicensingService _licensingService = licensingService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<ActivateLicenseCommandHandler> _logger = logger;

    public async Task<ActivateLicenseResult> Handle(
        ActivateLicenseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure user is authenticated
            if (!_currentUserService.UserId.HasValue)
            {
                _logger.LogWarning("Unauthenticated user attempted to activate license");
                return ActivateLicenseResult.FailureResult("User authentication required", 401);
            }

            if (!_currentUserService.BusinessId.HasValue)
            {
                _logger.LogWarning(
                    "User {UserId} without business attempted to activate license",
                    _currentUserService.UserId);
                return ActivateLicenseResult.FailureResult("Business not found", 404);
            }

            _logger.LogInformation(
                "Activating license for business {BusinessId} by user {UserId}",
                _currentUserService.BusinessId, _currentUserService.UserId);

            // Get business
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == _currentUserService.BusinessId.Value, cancellationToken);

            if (business == null)
            {
                _logger.LogWarning("Business {BusinessId} not found", _currentUserService.BusinessId);
                return ActivateLicenseResult.FailureResult("Business not found", 404);
            }

            // Validate deployment mode is OnPremise
            if (business.DeploymentMode != DeploymentMode.OnPremise)
            {
                _logger.LogWarning(
                    "Cannot activate license for business {BusinessId} with deployment mode {DeploymentMode}",
                    business.Id, business.DeploymentMode);

                return ActivateLicenseResult.FailureResult(
                    "License activation is only available for On-Premise deployments", 400);
            }

            // Call licensing service to validate the key
            var validationResult = await _licensingService.ValidateLicenseKeyAsync(
                request.LicenseKey,
                failOpen: false,  // FAIL-CLOSED: Block activation if service is down
                cancellationToken);


            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "License key validation failed for business {BusinessId}: {Message}",
                    business.Id, validationResult.Message);

                return ActivateLicenseResult.FailureResult(
                    validationResult.Message ?? "Invalid license key",
                    validationResult.StatusCode);
            }

            // Verify ClientId matches BusinessId
            if (!Guid.TryParse(validationResult.ClientId, out var clientBusinessId) ||
                clientBusinessId != business.Id)
            {
                _logger.LogError(
                    "License ClientId mismatch. Expected: {BusinessId}, Got: {ClientId}",
                    business.Id, validationResult.ClientId);

                return ActivateLicenseResult.FailureResult(
                    "This license key is not valid for your business", 403);
            }

            // Check if license is expired
            if (validationResult.ExpiryDate.HasValue && validationResult.ExpiryDate.Value < DateTime.UtcNow)
            {
                _logger.LogWarning(
                    "Attempted to activate expired license for business {BusinessId}. Expiry: {ExpiryDate}",
                    business.Id, validationResult.ExpiryDate);

                return ActivateLicenseResult.FailureResult(
                    $"This license key expired on {validationResult.ExpiryDate.Value:yyyy-MM-dd}", 400);
            }

            // Assign license to business
            var issuedDate = DateTime.UtcNow;
            var expiryDate = validationResult.ExpiryDate!.Value;

            business.AssignLicense(
                request.LicenseKey,
                issuedDate,
                expiryDate,
                _currentUserService.UserId.Value);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "License activated successfully for business {BusinessId}. Expires: {ExpiryDate}",
                business.Id, expiryDate);

            return ActivateLicenseResult.SuccessResult(
                request.LicenseKey,
                issuedDate,
                expiryDate,
                validationResult.Status ?? "Active");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error activating license for business {BusinessId}",
                _currentUserService.BusinessId);

            return ActivateLicenseResult.FailureResult(
                $"An error occurred while activating license: {ex.Message}",
                500);
        }
    }
}
