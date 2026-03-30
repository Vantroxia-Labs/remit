using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Application.Features.System.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Features.System.Handlers;

public class UpdateLicenseCommandHandler : IRequestHandler<UpdateLicenseCommand, UpdateLicenseResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateLicenseCommandHandler> _logger;

    public UpdateLicenseCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<UpdateLicenseCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<UpdateLicenseResult> Handle(UpdateLicenseCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var systemConfig = await _context.SystemConfigurations
                .FirstOrDefaultAsync(cancellationToken);

            if (systemConfig == null)
            {
                return new UpdateLicenseResult
                {
                    Success = false,
                    Message = "System configuration not found"
                };
            }

            if (systemConfig.DeploymentMode != Domain.Entities.DeploymentMode.OnPremise)
            {
                return new UpdateLicenseResult
                {
                    Success = false,
                    Message = "License updates are only available for On-Premise deployments"
                };
            }

            // Validate license key (in real implementation, this would call KMPG license validation service)
            if (!IsValidLicenseKey(request.LicenseKey))
            {
                return new UpdateLicenseResult
                {
                    Success = false,
                    Message = "Invalid license key provided"
                };
            }

            var currentUserId = _currentUserService.UserId ?? Guid.CreateVersion7();
            systemConfig.UpdateLicense(request.LicenseKey, request.ExpiryDate, currentUserId);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("License updated successfully for organization: {OrganizationName}", 
                systemConfig.OrganizationName);

            return new UpdateLicenseResult
            {
                Success = true,
                Message = "License updated successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating license");
            return new UpdateLicenseResult
            {
                Success = false,
                Message = "Failed to update license"
            };
        }
    }

    private static bool IsValidLicenseKey(string licenseKey)
    {
        // In real implementation, this would validate against KMPG's license server
        // For now, just check basic format
        return !string.IsNullOrWhiteSpace(licenseKey) && 
               licenseKey.Length >= 20 &&
               licenseKey.StartsWith("KMPG-");
    }
}