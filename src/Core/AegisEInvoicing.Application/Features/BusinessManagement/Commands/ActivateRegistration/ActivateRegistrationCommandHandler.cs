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

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateRegistration;

public class ActivateRegistrationCommandHandler(
    IApplicationDbContext context,
    IEmailService emailService,
    IApiKeyAuthenticationService apiKeyAuthenticationService,
    IConfiguration configuration,
    ILogger<ActivateRegistrationCommandHandler> logger,
    IPasswordHasher<SFTPUser> passwordHasher) : IRequestHandler<ActivateRegistrationCommand, ActivateRegistrationResult>
{
    private static readonly Guid SystemUserId = Guid.Parse("9c17ea5c-483c-44f8-97e8-c364e6739949");

    public async Task<ActivateRegistrationResult> Handle(ActivateRegistrationCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await context.BeginTransactionAsync(cancellationToken);

        try
        {
            var pending = await context.PendingBusinessRegistrations
                .FirstOrDefaultAsync(p => p.PaystackReference == request.PaystackReference && !p.IsDeleted, cancellationToken);

            if (pending is null)
                return new ActivateRegistrationResult(false, "Registration not found for this payment reference.");

            if (pending.Status == PendingRegistrationStatus.Activated)
                return new ActivateRegistrationResult(true, "Business already activated.", pending.ActivatedBusinessId, AlreadyActivated: true);

            if (pending.Status == PendingRegistrationStatus.Failed)
                return new ActivateRegistrationResult(false, "This registration has been marked as failed.");

            var plan = await context.PlatformSubscriptions
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pending.PlatformSubscriptionId && !p.IsDeleted, cancellationToken);

            if (plan is null)
                return new ActivateRegistrationResult(false, "Subscription plan not found.");

            pending.MarkPaid(request.PaidAt);
            await context.SaveChangesAsync(cancellationToken);

            // Create the business with placeholder details — admin fills in during onboarding
            var tin = TIN.Create("0000000000");
            var address = Address.Create("TBD", "TBD", "TBD", "Nigeria", "TBD");

            var business = Business.Create(
                name: pending.BusinessName,
                description: string.Empty,
                businessRegistrationNumber: "TBD",
                taxIdentificationNumber: tin,
                registeredAddress: address,
                invoicePrefix: "INV",
                contactEmail: pending.AdminEmail,
                adminUserId: null,
                createdBy: SystemUserId,
                contactPhone: pending.AdminPhone,
                serviceId: "TBD",
                industry: "TBD",
                firsBusinessId: Guid.Empty);

            await context.Businesses.AddAsync(business, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var tempPassword = GenerateSecurePassword();

            var clientAdminRole = await context.PlatformRoles
                .FirstOrDefaultAsync(r => r.Name == RoleConstants.ClientAdmin && !r.IsDeleted, cancellationToken);

            if (clientAdminRole is null)
                return new ActivateRegistrationResult(false, "System error: ClientAdmin role not found.");

            var adminUser = User.Create(
                businessId: business.Id,
                firstName: pending.AdminFirstName,
                lastName: pending.AdminLastName,
                email: pending.AdminEmail,
                passwordHash: PasswordHash.Create(tempPassword),
                createdBy: SystemUserId,
                branchId: null,
                phoneNumber: pending.AdminPhone);

            adminUser.AssignRole(clientAdminRole.Id, SystemUserId);
            await context.Users.AddAsync(adminUser, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            business.SetAdmin(adminUser.Id, SystemUserId);

            var defaultFlowRules = CreateDefaultFlowRules(business.Id, SystemUserId);
            await context.FlowRules.AddRangeAsync(defaultFlowRules, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            var startDate = DateTimeOffset.UtcNow;
            var endDate = pending.BillingCycle == BillingCycle.Annual
                ? startDate.AddYears(1)
                : startDate.AddMonths(1);

            var subscription = Subscription.Create(
                businessId: business.Id,
                platformSubscriptionId: plan.Id,
                startDate: startDate,
                endDate: endDate,
                createdBy: SystemUserId);

            subscription.UpdateBilling(startDate, endDate);
            await context.Subscriptions.AddAsync(subscription, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            business.AssignSubscription(subscription.Id, SystemUserId);
            await context.SaveChangesAsync(cancellationToken);

            string? apiKey = null;
            try { apiKey = await apiKeyAuthenticationService.GenerateApiKeyAsync(business.Id); }
            catch (Exception ex) { logger.LogError(ex, "Failed to generate API key for {BusinessId}", business.Id); }

            string? sftpPassword = null;
            if (plan.Tier == SubscriptionTier.SFTP)
            {
                try { sftpPassword = await CreateSftpUserAsync(business); }
                catch (Exception ex) { logger.LogError(ex, "Failed to create SFTP user for {BusinessId}", business.Id); }
            }

            pending.MarkActivated(business.Id);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            await SendWelcomeEmailAsync(pending, plan, business, tempPassword, apiKey, sftpPassword, cancellationToken);

            logger.LogInformation("Business {BusinessId} activated from registration {PendingId}", business.Id, pending.Id);

            return new ActivateRegistrationResult(true, "Business activated successfully.", business.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            logger.LogError(ex, "Error activating registration for reference {Reference}", request.PaystackReference);
            return new ActivateRegistrationResult(false, $"Activation failed: {ex.Message}");
        }
    }

    private async Task SendWelcomeEmailAsync(
        PendingBusinessRegistration pending,
        PlatformSubscription plan,
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

            string subject;
            string htmlBody;

            if (plan.Tier == SubscriptionTier.SaaS)
            {
                subject = "Welcome to Aegis NRS Portal — Your Account Details";
                htmlBody = BuildPortalWelcomeEmail(pending, tempPassword, portalUrl, supportEmail);
            }
            else if (plan.Tier == SubscriptionTier.SFTP)
            {
                subject = "Welcome to Aegis NRS Portal — Your Account & SFTP Details";
                htmlBody = BuildSftpWelcomeEmail(pending, business, tempPassword, sftpPassword, portalUrl, sftpHost, supportEmail);
            }
            else
            {
                subject = "Welcome to Aegis NRS Portal — Your Account & API Key";
                htmlBody = BuildApiWelcomeEmail(pending, tempPassword, apiKey, portalUrl, supportEmail);
            }

            await emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
            {
                Subject = subject,
                To = pending.AdminEmail,
                HtmlBody = htmlBody,
                TextBody = $"Welcome to Aegis NRS! Login at {portalUrl} with email: {pending.AdminEmail}"
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send welcome email to {Email}", pending.AdminEmail);
        }
    }

    private static string BuildPortalWelcomeEmail(PendingBusinessRegistration p, string tempPwd, string portalUrl, string supportEmail) =>
        $"""
        <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;">
          <div style="background:#1a5276;padding:24px;text-align:center;">
            <h1 style="color:white;margin:0;">Welcome to Aegis NRS</h1>
            <p style="color:#aed6f1;margin:4px 0 0;">Nigeria Revenue Service — Merchant Buyer Solution</p>
          </div>
          <div style="padding:32px;">
            <p>Dear {p.AdminFirstName},</p>
            <p>Your <strong>Portal Plan</strong> for <strong>{p.BusinessName}</strong> is now active.</p>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">Portal URL:</td><td><a href="{portalUrl}">{portalUrl}</a></td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Email:</td><td>{p.AdminEmail}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Temporary Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{tempPwd}</td></tr>
            </table>
            <p style="color:#e74c3c;"><strong>Important:</strong> You must change your password on first login.</p>
            <p>After signing in, complete your business profile (NRS credentials, TIN, Business ID, etc.) to start creating invoices.</p>
            <hr style="border:none;border-top:1px solid #eee;margin:24px 0;"/>
            <p style="font-size:12px;color:#999;">Support: <a href="mailto:{supportEmail}">{supportEmail}</a></p>
          </div>
        </body></html>
        """;

    private static string BuildSftpWelcomeEmail(PendingBusinessRegistration p, Business business, string tempPwd, string? sftpPwd, string portalUrl, string sftpHost, string supportEmail)
    {
        var sftpUsername = SanitizeUsername(business.Name);
        return $"""
        <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;">
          <div style="background:#1a5276;padding:24px;text-align:center;">
            <h1 style="color:white;margin:0;">Welcome to Aegis NRS</h1>
            <p style="color:#aed6f1;margin:4px 0 0;">Nigeria Revenue Service — Merchant Buyer Solution</p>
          </div>
          <div style="padding:32px;">
            <p>Dear {p.AdminFirstName},</p>
            <p>Your <strong>SFTP Plan</strong> for <strong>{p.BusinessName}</strong> is now active.</p>
            <h3>Portal Credentials</h3>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">Portal URL:</td><td><a href="{portalUrl}">{portalUrl}</a></td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Email:</td><td>{p.AdminEmail}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Temp Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{tempPwd}</td></tr>
            </table>
            <h3 style="margin-top:24px;">SFTP Credentials</h3>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">SFTP Host:</td><td>{sftpHost}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">SFTP Username:</td><td style="font-family:monospace;">{sftpUsername}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">SFTP Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{sftpPwd ?? "Contact support"}</td></tr>
            </table>
            <p style="color:#d35400;"><strong>Note:</strong> With the SFTP plan you upload invoices via SFTP. You can update payment status on the portal but cannot create invoices there.</p>
            <hr style="border:none;border-top:1px solid #eee;margin:24px 0;"/>
            <p style="font-size:12px;color:#999;">Support: <a href="mailto:{supportEmail}">{supportEmail}</a></p>
          </div>
        </body></html>
        """;
    }

    private static string BuildApiWelcomeEmail(PendingBusinessRegistration p, string tempPwd, string? apiKey, string portalUrl, string supportEmail) =>
        $"""
        <html><body style="font-family:Arial,sans-serif;color:#333;max-width:600px;margin:0 auto;">
          <div style="background:#1a5276;padding:24px;text-align:center;">
            <h1 style="color:white;margin:0;">Welcome to Aegis NRS</h1>
            <p style="color:#aed6f1;margin:4px 0 0;">Nigeria Revenue Service — Merchant Buyer Solution</p>
          </div>
          <div style="padding:32px;">
            <p>Dear {p.AdminFirstName},</p>
            <p>Your <strong>API Plan</strong> for <strong>{p.BusinessName}</strong> is now active.</p>
            <h3>Portal Credentials</h3>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="padding:8px 0;font-weight:bold;">Portal URL:</td><td><a href="{portalUrl}">{portalUrl}</a></td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Email:</td><td>{p.AdminEmail}</td></tr>
              <tr><td style="padding:8px 0;font-weight:bold;">Temp Password:</td><td style="font-family:monospace;background:#d5e8d4;padding:4px 8px;border-radius:4px;">{tempPwd}</td></tr>
            </table>
            <h3 style="margin-top:24px;">API Key</h3>
            <table style="background:#f4f6f7;border-radius:8px;padding:20px;width:100%;border-collapse:collapse;">
              <tr><td style="word-break:break-all;font-family:monospace;background:#d5e8d4;padding:8px;border-radius:4px;">{apiKey ?? "Available in portal settings"}</td></tr>
            </table>
            <p style="color:#d35400;"><strong>Note:</strong> With the API plan you submit invoices via the API. You can view invoices and update payment status on the portal, but cannot create invoices there.</p>
            <hr style="border:none;border-top:1px solid #eee;margin:24px 0;"/>
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
