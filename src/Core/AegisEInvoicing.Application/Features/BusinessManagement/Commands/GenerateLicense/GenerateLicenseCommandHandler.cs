using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.GenerateLicense;

/// <summary>
/// Handler for generating license keys for on-premise businesses
/// </summary>
public class GenerateLicenseCommandHandler(
    IApplicationDbContext context,
    ILicensingService licensingService,
    ICurrentUserService currentUserService,
    ILogger<GenerateLicenseCommandHandler> logger) : IRequestHandler<GenerateLicenseCommand, GenerateLicenseResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILicensingService _licensingService = licensingService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<GenerateLicenseCommandHandler> _logger = logger;

    public async Task<GenerateLicenseResult> Handle(
        GenerateLicenseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating license for business {BusinessId} by user {UserId}",
                request.BusinessId, _currentUserService.UserId);

            // Validate business exists
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId, cancellationToken);

            if (business == null)
            {
                _logger.LogWarning("Business {BusinessId} not found", request.BusinessId);
                return GenerateLicenseResult.FailureResult("Business not found", 404);
            }

            // Validate deployment mode is OnPremise
            if (business.DeploymentMode != DeploymentMode.OnPremise)
            {
                _logger.LogWarning(
                    "Cannot generate license for business {BusinessId} with deployment mode {DeploymentMode}",
                    request.BusinessId, business.DeploymentMode);

                return GenerateLicenseResult.FailureResult(
                    "License can only be generated for On-Premise deployments. This business is configured as SaaS.", 400);
            }

            // Validate expiry date is in the future
            if (request.ExpiryDate <= DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid expiry date {ExpiryDate} for business {BusinessId}",
                    request.ExpiryDate, request.BusinessId);

                return GenerateLicenseResult.FailureResult(
                    "License expiry date must be in the future", 400);
            }

            // Call licensing service to generate license
            var licenseResponse = await _licensingService.GenerateLicenseAsync(
                business.Id.ToString(),
                request.ExpiryDate,
                cancellationToken);

            if (licenseResponse.Status != 200 || string.IsNullOrWhiteSpace(licenseResponse.LicenseKey))
            {
                _logger.LogError(
                    "License generation failed for business {BusinessId}: {Message}",
                    request.BusinessId, licenseResponse.Message);

                return GenerateLicenseResult.FailureResult(
                    licenseResponse.Message ?? "License generation failed",
                    licenseResponse.Status);
            }

            // Update business with license information
            var issuedDate = DateTime.UtcNow;
            var expiryDateUtc = DateTime.SpecifyKind(request.ExpiryDate, DateTimeKind.Utc);

            var currentUserId = _currentUserService.UserId ?? Guid.Empty;

            business.AssignLicense(
                licenseResponse.LicenseKey,
                issuedDate,
                expiryDateUtc,
                currentUserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "License generated successfully for business {BusinessId}, expires {ExpiryDate}",
                request.BusinessId, request.ExpiryDate);

            return GenerateLicenseResult.SuccessResult(
                licenseResponse.LicenseKey,
                issuedDate,
                request.ExpiryDate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating license for business {BusinessId}",
                request.BusinessId);

            return GenerateLicenseResult.FailureResult(
                $"An error occurred while generating license: {ex.Message}",
                500);
        }
    }
}
