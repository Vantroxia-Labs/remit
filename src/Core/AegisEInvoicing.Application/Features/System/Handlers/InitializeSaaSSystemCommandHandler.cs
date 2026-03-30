using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Application.Features.System.Commands;
using EInvoiceIntegrator.Domain.Entities;
using EInvoiceIntegrator.Domain.Entities.UserManagement;
using EInvoiceIntegrator.Domain.ValueObjects.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Features.System.Handlers;

public class InitializeSaaSSystemCommandHandler : IRequestHandler<InitializeSaaSSystemCommand, SystemSetupResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InitializeSaaSSystemCommandHandler> _logger;

    public InitializeSaaSSystemCommandHandler(
        IApplicationDbContext context,
        ILogger<InitializeSaaSSystemCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SystemSetupResult> Handle(InitializeSaaSSystemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Check if system is already setup
            var existingConfig = await _context.SystemConfigurations
                .FirstOrDefaultAsync(cancellationToken);

            if (existingConfig != null)
            {
                return new SystemSetupResult
                {
                    Success = false,
                    Message = "System is already configured",
                    DeploymentMode = existingConfig.DeploymentMode
                };
            }

            // Create temporary admin user ID for system setup
            var tempAdminId = Guid.CreateVersion7();

            // Create system configuration
            var systemConfig = SystemConfiguration.CreateForSaaS(
                request.OrganizationName,
                tempAdminId,
                request.AllowSelfOnboarding,
                request.MaxBusinessesAllowed);

            _context.SystemConfigurations.Add(systemConfig);

            // Create admin user
            var passwordHash = PasswordHash.Create(request.AdminPassword);
            var adminUser = User.Create(
                Guid.CreateVersion7(), // Will be the KMPG business ID for SaaS
                request.AdminFirstName,
                request.AdminLastName,
                request.AdminEmail,
                passwordHash,
                tempAdminId,
                phoneNumber: null);

            _context.Users.Add(adminUser);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("SaaS system initialized successfully for organization: {OrganizationName}", 
                request.OrganizationName);

            return new SystemSetupResult
            {
                Success = true,
                Message = "SaaS system initialized successfully",
                AdminUserId = adminUser.Id,
                DeploymentMode = Domain.Entities.DeploymentMode.SaaS
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SaaS system for organization: {OrganizationName}", 
                request.OrganizationName);
            return new SystemSetupResult
            {
                Success = false,
                Message = "Failed to initialize SaaS system",
                DeploymentMode = Domain.Entities.DeploymentMode.SaaS
            };
        }
    }
}