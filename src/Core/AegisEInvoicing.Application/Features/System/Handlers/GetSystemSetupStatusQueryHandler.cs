using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Application.Features.System.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Features.System.Handlers;

public class GetSystemSetupStatusQueryHandler : IRequestHandler<GetSystemSetupStatusQuery, SystemSetupStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetSystemSetupStatusQueryHandler> _logger;

    public GetSystemSetupStatusQueryHandler(
        IApplicationDbContext context,
        ILogger<GetSystemSetupStatusQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SystemSetupStatusDto> Handle(GetSystemSetupStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var systemConfig = await _context.SystemConfigurations
                .FirstOrDefaultAsync(cancellationToken);

            if (systemConfig == null)
            {
                return new SystemSetupStatusDto
                {
                    IsSetupRequired = true,
                    IsSetupCompleted = false
                };
            }

            return new SystemSetupStatusDto
            {
                IsSetupRequired = false,
                IsSetupCompleted = systemConfig.IsSetupCompleted,
                DeploymentMode = systemConfig.DeploymentMode,
                OrganizationName = systemConfig.OrganizationName,
                SetupCompletedAt = systemConfig.SetupCompletedAt,
                IsLicenseValid = systemConfig.DeploymentMode == Domain.Entities.DeploymentMode.OnPremise 
                    ? systemConfig.IsLicenseValid() 
                    : null,
                LicenseExpiryDate = systemConfig.LicenseExpiryDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system setup status");
            throw;
        }
    }
}