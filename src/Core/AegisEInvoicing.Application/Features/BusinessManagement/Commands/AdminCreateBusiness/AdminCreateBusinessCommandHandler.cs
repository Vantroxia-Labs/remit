using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services.Authentication;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.AdminCreateBusiness;

public class AdminCreateBusinessCommandHandler(
    IApplicationDbContext context,
    IEmailService emailService,
    IApiKeyAuthenticationService apiKeyAuthenticationService,
    IConfiguration configuration,
    ILogger<AdminCreateBusinessCommandHandler> logger,
    IPasswordHasher<SFTPUser> passwordHasher) : IRequestHandler<AdminCreateBusinessCommand, AdminCreateBusinessResult>
{
    private static readonly Guid SystemUserId = Guid.Parse("9c17ea5c-483c-44f8-97e8-c364e6739949");

    public async Task<AdminCreateBusinessResult> Handle(AdminCreateBusinessCommand request, CancellationToken cancellationToken)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.BeginTransactionAsync(cancellationToken);

            try
            {
                // Check for duplicate email
                var existingUser = await context.Users
                    .AsNoTracking()
                    .AnyAsync(u => u.Email == request.AdminEmail.ToLowerInvariant() && !u.IsDeleted, cancellationToken);

                if (existingUser)
                    return new AdminCreateBusinessResult(false, "An account with this email already exists.");

                if (request.PlatformSubscriptionIds == null || request.PlatformSubscriptionIds.Count == 0)
                    return new AdminCreateBusinessResult(false, "At least one subscription plan must be selected.");

                // Validate all plans exist
                var planIds = request.PlatformSubscriptionIds.Distinct().ToList();
                var plans = await context.PlatformSubscriptions
                    .AsNoTracking()
                    .Where(p => planIds.Contains(p.Id) && !p.IsDeleted)
                    .ToListAsync(cancellationToken);

                if (plans.Count != planIds.Count)
                    return new AdminCreateBusinessResult(false, "One or more selected subscription plans are not valid.");

                // Create business
                var tin = TIN.Create(request.Tin?.Trim() ?? "0000000000");
                var address = Address.Create(string.Empty, string.Empty, string.Empty, "Nigeria", string.Empty);

                var firsBusinessId = !string.IsNullOrWhiteSpace(request.NRSBusinessId) &&
                    Guid.TryParse(request.NRSBusinessId, out var parsedFirsId)
                        ? parsedFirsId
                        : Guid.Empty;

                var business = Business.Create(
                    name: request.BusinessName,
                    description: request.BusinessDescription,
                    businessRegistrationNumber: request.BusinessRegistrationNumber?.Trim() ?? string.Empty,
                    taxIdentificationNumber: tin,
                    registeredAddress: address,
                    invoicePrefix: "INV",
                    contactEmail: request.AdminEmail.ToLowerInvariant(),
                    adminUserId: null,
                    createdBy: SystemUserId,
                    contactPhone: request.AdminPhone,
                    serviceId: request.ServiceId?.Trim() ?? string.Empty,
                    industry: request.Industry ?? "Other",
                    firsBusinessId: firsBusinessId);

                // Record payment details for audit
                business.SetAdminPayment(request.PaymentReference.Trim(), request.PaymentAmountNaira, SystemUserId);

                await context.Businesses.AddAsync(business, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                var tempPassword = GenerateSecurePassword();

                var clientAdminRole = await context.PlatformRoles
                    .FirstOrDefaultAsync(r => r.Name == RoleConstants.ClientAdmin && !r.IsDeleted, cancellationToken);

                if (clientAdminRole is null)
                    return new AdminCreateBusinessResult(false, "System error: ClientAdmin role not found.");

                var adminUser = User.Create(
                    businessId: business.Id,
                    firstName: request.AdminFirstName,
                    lastName: request.AdminLastName,
                    email: request.AdminEmail.ToLowerInvariant(),
                    passwordHash: PasswordHash.Create(tempPassword),
                    createdBy: SystemUserId,
                    branchId: null,
                    phoneNumber: request.AdminPhone);

                adminUser.AssignRole(clientAdminRole.Id, SystemUserId);
                await context.Users.AddAsync(adminUser, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                business.SetAdmin(adminUser.Id, SystemUserId);

                var defaultFlowRules = CreateDefaultFlowRules(business.Id, SystemUserId);
                await context.FlowRules.AddRangeAsync(defaultFlowRules, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);

                var startDate = DateTimeOffset.UtcNow;
                var endDate = request.BillingCycle == BillingCycle.Annual
                    ? startDate.AddYears(1)
                    : startDate.AddMonths(1);

                // Create one Subscription record per selected plan
                foreach (var plan in plans)
                {
                    var subscription = Subscription.Create(
                        businessId: business.Id,
                        platformSubscriptionId: plan.Id,
                        startDate: startDate,
                        endDate: endDate,
                        createdBy: SystemUserId);

                    subscription.UpdateBilling(startDate, endDate);
                    await context.Subscriptions.AddAsync(subscription, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                }

                string? apiKey = null;
                try { apiKey = await apiKeyAuthenticationService.GenerateApiKeyAsync(business.Id); }
                catch (Exception ex) { logger.LogError(ex, "Failed to generate API key for {BusinessId}", business.Id); }

                string? sftpPassword = null;
                if (plans.Any(p => p.Tier == SubscriptionTier.SFTP))
                {
                    try { sftpPassword = await CreateSftpUserAsync(business); }
                    catch (Exception ex) { logger.LogError(ex, "Failed to create SFTP user for {BusinessId}", business.Id); }
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                await SendWelcomeEmailAsync(request, plans, business, tempPassword, apiKey, sftpPassword, cancellationToken);

                logger.LogInformation("Business {BusinessId} ({Name}) created by Aegis admin. PayRef: {Ref}", business.Id, business.Name, request.PaymentReference);

                return new AdminCreateBusinessResult(true, "Business created successfully.", business.Id);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                logger.LogError(ex, "Error creating business for {Email}", request.AdminEmail);
                return new AdminCreateBusinessResult(false, $"Failed to create business: {ex.Message}");
            }
        }); // end ExecuteAsync
    }

    private async Task SendWelcomeEmailAsync(
        AdminCreateBusinessCommand request,
        List<PlatformSubscription> plans,
        Business business,
        string tempPassword,
        string? apiKey,
        string? sftpPassword,
        CancellationToken cancellationToken)
    {
        try
        {
            var portalUrl = configuration["App:PortalUrl"] ?? "https://portal.aegisnrs.com";
            var supportEmail = configuration["Support:Email"] ?? "support@aegisnrs.com";
            var sftpHost = configuration["SftpConfiguration:Host"] ?? "sftp.aegisnrs.com";
            var planNames = string.Join(" + ", plans.Select(p => p.PlanName));

            var hasSftp = plans.Any(p => p.Tier == SubscriptionTier.SFTP);
            var hasSaas = plans.Any(p => p.Tier == SubscriptionTier.SaaS);

            string subject = $"Welcome to Aegis EInvoicing Platform — {planNames} Account Details";
            string htmlBody;

            if (hasSftp)
                htmlBody = BuildSftpWelcomeEmail(request, business, tempPassword, sftpPassword, portalUrl, sftpHost, supportEmail, planNames);
            else if (hasSaas)
                htmlBody = BuildPortalWelcomeEmail(request, tempPassword, portalUrl, supportEmail, planNames);
            else
                htmlBody = BuildApiWelcomeEmail(request, tempPassword, apiKey, portalUrl, supportEmail, planNames);

            await emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
            {
                Subject = subject,
                To = request.AdminEmail,
                HtmlBody = htmlBody,
                TextBody = $"Welcome to Aegis NRS! Login at {portalUrl} with email: {request.AdminEmail}"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", request.AdminEmail);
        }
    }

    private static string BuildPortalWelcomeEmail(AdminCreateBusinessCommand r, string tempPwd, string portalUrl, string supportEmail, string planNames) =>
        $"""
        <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;">
          <div style="background:#1a5276;padding:24px;text-align:center;">
            <h1 style="color:white;margin:0;">Welcome to Aegis NRS</h1>
          </div>
          <div style="padding:32px;">
            <p>Dear {r.AdminFirstName},</p>
            <p>Your <strong>{planNames}</strong> account for <strong>{r.BusinessName}</strong> is now active.</p>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">Portal URL:</td><td><a href="{portalUrl}">{portalUrl}</a></td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Email:</td><td>{r.AdminEmail}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Temporary Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{tempPwd}</td></tr>
            </table>
            <p style="color:#e74c3c;"><strong>Important:</strong> You must change your password on first login.</p>
            <p style="font-size:12px;color:#999;">Support: <a href="mailto:{supportEmail}">{supportEmail}</a></p>
          </div>
        </body></html>
        """;

    private static string BuildSftpWelcomeEmail(AdminCreateBusinessCommand r, Business business, string tempPwd, string? sftpPwd, string portalUrl, string sftpHost, string supportEmail, string planNames)
    {
        var sftpUsername = SanitizeUsername(business.Name);
        return $"""
        <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;">
          <div style="background:#1a5276;padding:24px;text-align:center;">
            <h1 style="color:white;margin:0;">Welcome to Aegis NRS</h1>
          </div>
          <div style="padding:32px;">
            <p>Dear {r.AdminFirstName},</p>
            <p>Your <strong>{planNames}</strong> account for <strong>{r.BusinessName}</strong> is now active.</p>
            <h3>Portal Credentials</h3>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">Portal URL:</td><td><a href="{portalUrl}">{portalUrl}</a></td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Email:</td><td>{r.AdminEmail}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Temp Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{tempPwd}</td></tr>
            </table>
            <h3 style="margin-top:24px;">SFTP Credentials</h3>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">SFTP Host:</td><td>{sftpHost}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Username:</td><td style="font-family:monospace;">{sftpUsername}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{sftpPwd ?? "Contact support"}</td></tr>
            </table>
            <p style="font-size:12px;color:#999;">Support: <a href="mailto:{supportEmail}">{supportEmail}</a></p>
          </div>
        </body></html>
        """;
    }

    private static string BuildApiWelcomeEmail(AdminCreateBusinessCommand r, string tempPwd, string? apiKey, string portalUrl, string supportEmail, string planNames) =>
        $"""
        <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;">
          <div style="background:#1a5276;padding:24px;text-align:center;">
            <h1 style="color:white;margin:0;">Welcome to Aegis NRS</h1>
          </div>
          <div style="padding:32px;">
            <p>Dear {r.AdminFirstName},</p>
            <p>Your <strong>{planNames}</strong> account for <strong>{r.BusinessName}</strong> is now active.</p>
            <h3>Portal Credentials</h3>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">Portal URL:</td><td><a href="{portalUrl}">{portalUrl}</a></td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Email:</td><td>{r.AdminEmail}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Temp Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{tempPwd}</td></tr>
            </table>
            <h3 style="margin-top:24px;">API Key</h3>
            <p style="font-family:monospace;background:#d5e8d4;padding:8px;border-radius:4px;word-break:break-all;">{apiKey ?? "Available in portal settings"}</p>
            <p style="font-size:12px;color:#999;">Support: <a href="mailto:{supportEmail}">{supportEmail}</a></p>
          </div>
        </body></html>
        """;

    private async Task<string> CreateSftpUserAsync(Business business)
    {
        var ftpRootPath = configuration["SftpConfiguration:FtpRootPath"] ?? "C:\\ftproot";
        var sftpUsername = SanitizeUsername(business.Name);
        var sftpPassword = GenerateSecurePassword();
        var hashedPassword = passwordHasher.HashPassword(null!, sftpPassword);
        var userRootPath = Path.Combine(ftpRootPath, "uploads", business.Id.ToString());
        var workingDirectory = $"/uploads/{business.Id}/In-Progress";

        var sftpUser = SFTPUser.Create(business.Id, sftpUsername, hashedPassword, userRootPath, workingDirectory, SystemUserId);
        sftpUser.MarkDirectoriesAsCreated(SystemUserId);

        await context.SFTPUsers.AddAsync(sftpUser);
        await context.SaveChangesAsync();
        return sftpPassword;
    }

    private static List<FlowRule> CreateDefaultFlowRules(Guid businessId, Guid createdBy) =>
    [
        FlowRule.CreateWithRange("Auto-Approve Small Invoices (Default)",
            "Automatically approve invoices up to ₦1,000,000.", 0.01m, 1_000_000m,
            false, 2, businessId, createdBy, false, null, null, null),
        FlowRule.CreateWithRange("Require Approval for Large Invoices (Default)",
            "Invoices above ₦1,000,000 require ClientAdmin approval.", 1_000_000.01m, 999_999_999_999m,
            true, 1, businessId, createdBy, false, null, null, null)
    ];

    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()_+-=";
        var random = new Random();
        var pwd = new char[16];
        pwd[0] = upper[random.Next(upper.Length)];
        pwd[1] = lower[random.Next(lower.Length)];
        pwd[2] = digits[random.Next(digits.Length)];
        pwd[3] = special[random.Next(special.Length)];
        var all = upper + lower + digits + special;
        for (int i = 4; i < pwd.Length; i++) pwd[i] = all[random.Next(all.Length)];
        for (int i = pwd.Length - 1; i > 0; i--) { int j = random.Next(i + 1); (pwd[i], pwd[j]) = (pwd[j], pwd[i]); }
        return new string(pwd);
    }

    private static string SanitizeUsername(string name)
    {
        var s = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9]", "_").ToLower().Trim('_');
        if (s.Length > 0 && char.IsDigit(s[0])) s = "user_" + s;
        return s.Length > 50 ? s[..50].TrimEnd('_') : s;
    }
}
