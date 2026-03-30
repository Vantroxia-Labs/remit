using EInvoiceIntegrator.Application.Common.Interfaces;
using EInvoiceIntegrator.Application.Features.System.Commands;
using EInvoiceIntegrator.Domain.Entities;
using EInvoiceIntegrator.Domain.Entities.BusinessManagement;
using EInvoiceIntegrator.Domain.Entities.UserManagement;
using EInvoiceIntegrator.Domain.ValueObjects;
using EInvoiceIntegrator.Domain.ValueObjects.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EInvoiceIntegrator.Application.Features.System.Handlers;

public class InitializeOnPremiseSystemCommandHandler : IRequestHandler<InitializeOnPremiseSystemCommand, SystemSetupResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InitializeOnPremiseSystemCommandHandler> _logger;

    public InitializeOnPremiseSystemCommandHandler(
        IApplicationDbContext context,
        ILogger<InitializeOnPremiseSystemCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SystemSetupResult> Handle(InitializeOnPremiseSystemCommand request, CancellationToken cancellationToken)
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

            // Get and mark subscription key as used
            var subscriptionKey = await _context.SubscriptionKeys
                .FirstOrDefaultAsync(sk => sk.Id == request.SubscriptionKeyId, cancellationToken);

            if (subscriptionKey == null)
            {
                return new SystemSetupResult
                {
                    Success = false,
                    Message = "Subscription key not found",
                    DeploymentMode = Domain.Entities.DeploymentMode.OnPremise
                };
            }

            // Create temporary admin user ID for system setup
            var tempAdminId = Guid.CreateVersion7();

            // Mark subscription key as used
            subscriptionKey.MarkAsUsed(tempAdminId, $"Used for on-premise setup: {request.OrganizationName}");

            // Create system configuration using subscription key data
            var systemConfig = SystemConfiguration.CreateForOnPremise(
                request.OrganizationName,
                request.LicenseKey,
                subscriptionKey.ExpiryDate,
                request.ContactEmail,
                request.ContactPhone,
                tempAdminId);

            _context.SystemConfigurations.Add(systemConfig);

            // Create the organization's business entity
            var businessId = Guid.CreateVersion7();
            var tin = TIN.Create("ONPREMISE-" + DateTime.UtcNow.ToString("yyyyMMdd")); // Placeholder TIN
            var address = Address.Create("HQ Address", "City", "State", "Country", "00000"); // Placeholder

            var business = Business.Create(
                request.OrganizationName,
                $"{request.OrganizationName} On-Premise Installation",
                "ON-PREMISE-REG",
                tin,
                address,
                request.ContactEmail,
                tempAdminId,
                tempAdminId,
                request.ContactPhone,
                "ON-PREMISE-FIRS-SERVICE",
                "ON-PREMISE-FIRS-SECRET");

            _context.Businesses.Add(business);

            // Create admin user for the organization
            var passwordHash = PasswordHash.Create(request.AdminPassword);
            var adminUser = User.Create(
                businessId,
                request.AdminFirstName,
                request.AdminLastName,
                request.AdminEmail,
                passwordHash,
                tempAdminId,
                phoneNumber: request.ContactPhone);

            _context.Users.Add(adminUser);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("On-Premise system initialized successfully for organization: {OrganizationName}", 
                request.OrganizationName);

            return new SystemSetupResult
            {
                Success = true,
                Message = "On-Premise system initialized successfully",
                AdminUserId = adminUser.Id,
                DeploymentMode = Domain.Entities.DeploymentMode.OnPremise
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing On-Premise system for organization: {OrganizationName}", 
                request.OrganizationName);
            return new SystemSetupResult
            {
                Success = false,
                Message = "Failed to initialize On-Premise system",
                DeploymentMode = Domain.Entities.DeploymentMode.OnPremise
            };
        }
    }

}