using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services.Authentication;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.OnboardBusiness;

public class OnboardBusinessCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEmailService emailService,
    IApiKeyAuthenticationService apiKeyAuthenticationService,
    IConfiguration configuration,
    ILogger<OnboardBusinessCommandHandler> logger,
    IPasswordHasher<SFTPUser> passwordHasher
    ) : IRequestHandler<OnboardBusinessCommand, OnboardBusinessResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly IApiKeyAuthenticationService _apiKeyAuthenticationService = apiKeyAuthenticationService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OnboardBusinessCommandHandler> _logger = logger;
    private readonly IPasswordHasher<SFTPUser> _passwordHasher = passwordHasher;

    // TODO Wrap in a transaction

    public async Task<OnboardBusinessResult> Handle(OnboardBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue)
                return new OnboardBusinessResult(false, "User authentication required");

            var tin = TIN.Create(request.TIN);

            var business = Business.Create(
                request.BusinessName,
                request.Description,
                request.BusinessRegistrationNumber,
                tin,
                request.RegisteredAddress,
                request.InvoicePrefix,
                request.ContactEmail,
                null,
                _currentUser.UserId.Value,
                request.ContactPhone,
                request.ServiceId,
                request.Industry,
                request.FIRSBusinessId);

            // Set deployment mode if provided (Aegis admin can specify OnPremise or SaaS)
            if (request.DeploymentMode.HasValue)
                business.SetDeploymentMode(request.DeploymentMode.Value, _currentUser.UserId.Value);

            await _context.Businesses.AddAsync(business, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Generate a strong temporary password for the admin user
            var tempPassword = GenerateSecurePassword();
            var adminUser = User.Create(
                business.Id,
                request.AdminFirstName,
                request.AdminLastName,
                request.ContactEmail,
                PasswordHash.Create(tempPassword),
                _currentUser.UserId.Value,
                null,
                request.ContactPhone);

            // Add the admin user to the database
            await _context.Users.AddAsync(adminUser, cancellationToken);

            var role = await _context.PlatformRoles.SingleOrDefaultAsync(x => x.Name == RoleConstants.ClientAdmin);
            var PlatformSubscription=await _context.PlatformSubscriptions.SingleOrDefaultAsync(x => x.Id==request.PlatformSubscriptionId && !x.IsDeleted);

            if(PlatformSubscription is null)
            {
                return new OnboardBusinessResult(
                    false,
                    $"Selected Subscription is not Valid");
            }

            if (role is null)
            {
                return new OnboardBusinessResult(
                     false,
                     $"Onboarding Failed. Please try again later");
            }

            adminUser.AssignRole(role.Id, _currentUser.UserId.Value);
            await _context.SaveChangesAsync(cancellationToken);

            // Update business with the correct admin user ID
            business.SetAdmin(adminUser.Id, _currentUser.UserId.Value);

            // Create default FlowRules for the business
            _logger.LogInformation("Creating default FlowRules for business: {BusinessId}", business.Id);
            var defaultFlowRules = CreateDefaultFlowRules(business.Id, _currentUser.UserId.Value);
            await _context.FlowRules.AddRangeAsync(defaultFlowRules, cancellationToken);

            // Save all changes
            await _context.SaveChangesAsync(cancellationToken);

            var subscription = Subscription.Create(
                business.Id,
                request.PlatformSubscriptionId,
                request.SubscriptionStartDateTimeOffset,
                request.SubscriptionEndDateTimeOffset,
                _currentUser.UserId.Value);

            subscription.UpdateBilling(subscription.StartDate, subscription.EndDate);

            await _context.Subscriptions.AddAsync(subscription, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            business.AssignSubscription(subscription.Id, _currentUser.UserId.Value);
            await _context.SaveChangesAsync(cancellationToken);

            // Generate API key for the business
            string? apiKey = null;
            try
            {
                apiKey = await _apiKeyAuthenticationService.GenerateApiKeyAsync(business.Id);
                _logger.LogInformation("API key generated successfully for business: {BusinessId}", business.Id);
            }
            catch (Exception apiKeyEx)
            {
                _logger.LogError(apiKeyEx, "Failed to generate API key for business: {BusinessId}. Error: {Error}", business.Id, apiKeyEx.Message);
                // Don't fail the entire onboarding process if API key generation fails
                // The business can still be onboarded and API key can be generated later
            }

            // Create SFTP user in SFTPGo and file system - Only for SaaS subscriptions
            string? sftpPassword = null;
            if (PlatformSubscription?.Tier == Domain.Entities.SubscriptionTier.SaaS)
            {
                try
                {
                    sftpPassword = await CreateSftpUserAsync(business, request);
                    _logger.LogInformation("SFTP user created successfully for business: {BusinessId}", business.Id);
                }
                catch (Exception sftpEx)
                {
                    _logger.LogError(sftpEx, "Failed to create SFTP user for business: {BusinessId}. Error: {Error}", business.Id, sftpEx.Message);
                    // Continue with onboarding even if SFTP creation fails
                    // The SFTP user can be created manually later if needed
                }
            }
            else
            {
                _logger.LogInformation("SFTP user creation skipped for business: {BusinessId}. Subscription tier: {Tier}", 
                    business.Id, PlatformSubscription?.Tier);
            }

            // Send welcome email with credentials
            try
            {
                var emailBody = string.Empty;

                if (PlatformSubscription?.Tier == Domain.Entities.SubscriptionTier.SaaS)
                {
                    // SaaS tier gets full welcome email with SFTP credentials
                    emailBody = CreateBasicWelcomeEmailBody(request.BusinessName, request.AdminFirstName, request.ContactEmail, tempPassword, sftpPassword);
                }
                else if (PlatformSubscription?.Tier == Domain.Entities.SubscriptionTier.ApiOnly)
                {
                    // ApiOnly tier gets email with API key but no SFTP credentials
                    emailBody = CreateWelcomeEmailBody(request.BusinessName, business.Id, request.AdminFirstName, request.ContactEmail, tempPassword, apiKey, null);
                }
                else
                {
                    // Default/OnPremise tier gets basic email without SFTP
                    emailBody = CreateBasicWelcomeEmailBody(request.BusinessName, request.AdminFirstName, request.ContactEmail, tempPassword, null);
                }

                await _emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
                {
                    Subject = "Welcome to EInvoice Integrator - Your Account Details",
                    To = request.ContactEmail,
                    HtmlBody = emailBody,
                    TextBody = $"Welcome to EInvoice Integrator! Your account has been created. Login Email: {request.ContactEmail}"
                }, cancellationToken);

                _logger.LogInformation("Welcome email sent successfully to: {Email}", request.ContactEmail);
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send welcome email to: {Email}. Error: {Error}", request.ContactEmail, emailEx.Message);
            }

            return new OnboardBusinessResult(
                true,
                "Business successfully onboarded to EInvoice Integrator platform. KMPG manages all FIRS interactions.",
                business.Id,
                "KMPG Managed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new OnboardBusinessResult(
                false,
                $"Failed to onboard business: {ex.Message}");
        }
    }

    private static string GenerateSecurePassword()
    {
        const string upperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowerCase = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var random = new Random();
        var password = new char[16];

        // Ensure at least one character from each category
        password[0] = upperCase[random.Next(upperCase.Length)];
        password[1] = lowerCase[random.Next(lowerCase.Length)];
        password[2] = digits[random.Next(digits.Length)];
        password[3] = specialChars[random.Next(specialChars.Length)];

        // Fill the rest with random characters from all categories
        var allChars = upperCase + lowerCase + digits + specialChars;
        for (int i = 4; i < password.Length; i++)
        {
            password[i] = allChars[random.Next(allChars.Length)];
        }

        // Shuffle the password to avoid predictable patterns
        for (int i = password.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (password[i], password[j]) = (password[j], password[i]);
        }

        return new string(password);
    }

    /// <summary>
    /// Creates an SFTP user in the database for SFTPGo authentication
    /// </summary>
    private async Task<string> CreateSftpUserAsync(Business business, OnboardBusinessCommand request)
    {
        var ftpRootPath = _configuration["SftpConfiguration:FtpRootPath"] ?? "C:\\ftproot";
        var sftpUsername = SanitizeUsername(business.Name);
        var sftpPassword = GenerateSecurePassword();

        try
        {
            _logger.LogInformation("Creating SFTP user in database for SFTPGo: {Username}", sftpUsername);

            var userRootPath = Path.Combine(ftpRootPath, "uploads", business.Id.ToString());
            var workingDirectory = $"/uploads/{business.Id}/In-Progress";

            var hashedPassword = _passwordHasher.HashPassword(null!, sftpPassword);

            var sftpUser = SFTPUser.Create(
                business.Id,
                sftpUsername,
                hashedPassword,
                userRootPath,
                workingDirectory,
                _currentUser.UserId!.Value);

            sftpUser.MarkDirectoriesAsCreated(_currentUser.UserId!.Value);

            await _context.SFTPUsers.AddAsync(sftpUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("SFTP user {Username} created in database for business {BusinessId}", 
                sftpUsername, business.Id);

            return sftpPassword;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SFTP user for business {BusinessId}", business.Id);
            throw;
        }
    }
    
    /// <summary>
    /// Sanitizes business name for use as SFTP username
    /// </summary>
    private static string SanitizeUsername(string businessName)
    {
        // Remove special characters and spaces, replace with underscores
        var sanitized = System.Text.RegularExpressions.Regex.Replace(businessName, @"[^a-zA-Z0-9]", "_")
            .ToLower()
            .Trim('_');
        
        // Ensure it starts with a letter and is not too long
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "user_" + sanitized;
        }
        
        if (sanitized.Length > 50)
        {
            sanitized = sanitized[..50].TrimEnd('_');
        }
        
        return sanitized;
    }

    private string CreateWelcomeEmailBody(string businessName, Guid AegisBusinessId, string adminFirstName, string email, string password, string? apiKey, string? sftpPassword)
    {
        var supportEmail = _configuration["Support:Email"] ?? "support@Aegis.com";
        var sftpUsername = SanitizeUsername(businessName);

        // Load the template
        var template = LoadEmailTemplate("WelcomeEmail.html");

        // Replace all placeholders
        return template
            .Replace("{adminFirstName}", adminFirstName)
            .Replace("{businessName}", businessName)
            .Replace("{AegisBusinessId}", AegisBusinessId.ToString())
            .Replace("{email}", email)
            .Replace("{password}", password)
            .Replace("{apiKey}", apiKey ?? "")
            .Replace("{sftpUsername}", sftpUsername)
            .Replace("{sftpPassword}", sftpPassword ?? "")
            .Replace("{supportEmail}", supportEmail);
    }

    private string CreateBasicWelcomeEmailBody(string businessName, string adminFirstName, string email, string password, string? sftpPassword)
    {
        var supportEmail = _configuration["Support:Email"] ?? "support@Aegis.com";
        var sftpUsername = SanitizeUsername(businessName);

        // Load the template
        var template = LoadEmailTemplate("WelcomeEmailBasic.html");

        // Replace all placeholders
        return template
            .Replace("{adminFirstName}", adminFirstName)
            .Replace("{businessName}", businessName)
            .Replace("{email}", email)
            .Replace("{password}", password)
            .Replace("{sftpUsername}", sftpUsername)
            .Replace("{sftpPassword}", sftpPassword ?? "")
            .Replace("{supportEmail}", supportEmail);
    }

    private static string LoadEmailTemplate(string templateName)
    {
        var assembly = typeof(OnboardBusinessCommandHandler).Assembly;
        var resourceName = $"AegisEInvoicing.Application.Templates.Email.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException($"Email template not found: {templateName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Creates default FlowRules for a new business
    /// Rule 1: Auto-approve invoices up to 1 million
    /// Rule 2: Require approval for invoices above 1 million
    /// </summary>
    private static List<FlowRule> CreateDefaultFlowRules(Guid businessId, Guid createdBy)
    {
        var flowRules = new List<FlowRule>();

        // Rule 1: Auto-approve small invoices (0.01 - 1,000,000)
        var autoApproveRule = FlowRule.CreateWithRange(
            name: "Auto-Approve Small Invoices (Default)",
            description: "Automatically approve invoices up to ?1,000,000. You can modify or delete this rule anytime.",
            minAmount: 0.01m,
            maxAmount: 1_000_000m,
            requiresClientAdminApproval: false,
            priority: 2,
            businessId: businessId,
            createdBy: createdBy,
            enableTimeBasedRules: false,
            activeStartTime: null,
            activeEndTime: null,
            activeDaysOfWeek: null);

        flowRules.Add(autoApproveRule);

        // Rule 2: Require approval for large invoices (1,000,000.01+)
        var requireApprovalRule = FlowRule.CreateWithRange(
            name: "Require Approval for Large Invoices (Default)",
            description: "Invoices above ?1,000,000 require ClientAdmin approval. You can modify or delete this rule anytime.",
            minAmount: 1_000_000.01m,
            maxAmount: 999_999_999_999m,
            requiresClientAdminApproval: true,
            priority: 1,
            businessId: businessId,
            createdBy: createdBy,
            enableTimeBasedRules: false,
            activeStartTime: null,
            activeEndTime: null,
            activeDaysOfWeek: null);

        flowRules.Add(requireApprovalRule);

        return flowRules;
    }
}

