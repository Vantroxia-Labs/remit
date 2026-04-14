using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.SendActionOtp;

public class SendActionOtpCommandHandler(
    IApplicationDbContext context,
    IEmailService emailService,
    ITotpService otpService,
    ICurrentUserService currentUserService,
    IConfiguration configuration,
    ILogger<SendActionOtpCommandHandler> logger) : IRequestHandler<SendActionOtpCommand, SendActionOtpResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly ITotpService _otpService = otpService;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<SendActionOtpCommandHandler> _logger = logger;

    public async Task<SendActionOtpResult> Handle(SendActionOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUserService.UserId;
            if (!userId.HasValue)
                return new SendActionOtpResult(false, "User not authenticated.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);

            if (user is null)
                return new SendActionOtpResult(false, "User account not found.");

            if (string.IsNullOrWhiteSpace(user.Email))
                return new SendActionOtpResult(false, "No email address associated with this account.");

            var otp = _otpService.Generate($"{userId.Value}");

            // Ensure at least 6 digits
            while (otp.ToString().Length < 6)
            {
                otp = _otpService.Generate($"{userId.Value}");
            }

            // Load OTP email template
            var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Email", "OtpVerificationEmail.html");
            var emailBody = await File.ReadAllTextAsync(templatePath, cancellationToken);

            var supportEmail = _configuration["Support:Email"] ?? "support@aegisnrs.com";
            emailBody = emailBody
                .Replace("{otp}", otp.ToString())
                .Replace("{supportEmail}", supportEmail);

            await _emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
            {
                Subject = "Your Action Verification Code",
                To = user.Email,
                HtmlBody = emailBody
            });

            _logger.LogInformation("Action OTP sent to {Email} for user {UserId}", user.Email, userId.Value);

            return new SendActionOtpResult(true, "OTP sent successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending action OTP for user {UserId}", _currentUserService.UserId);
            return new SendActionOtpResult(false, "Something went wrong.");
        }
    }
}
