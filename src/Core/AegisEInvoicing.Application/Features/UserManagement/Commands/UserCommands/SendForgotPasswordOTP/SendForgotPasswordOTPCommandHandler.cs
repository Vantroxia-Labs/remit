using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateBusiness;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.RequestChangePassword;

public class SendForgotPasswordOTPCommandHandler(
	IApplicationDbContext context,
	IEmailService emailService, 
	ITotpService otpService,
	IConfiguration configuration,
	ILogger<SendForgotPasswordOTPCommandHandler> logger) : IRequestHandler<SendForgotPasswordOTPCommand, SendForgotPasswordOTPResult>
{
	private readonly IApplicationDbContext _context = context;
	private readonly IEmailService _emailService = emailService;
	private readonly ILogger<SendForgotPasswordOTPCommandHandler> _logger = logger;
	private readonly ITotpService _otpService = otpService;
	private readonly IConfiguration _configuration = configuration;

	public async Task<SendForgotPasswordOTPResult> Handle(SendForgotPasswordOTPCommand request, CancellationToken cancellationToken)
	{
		try
		{
			var userAccount = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.phone_email, cancellationToken);

			if(userAccount is null)
			{
				userAccount = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.phone_email, cancellationToken);

				if(userAccount is null)
					return new SendForgotPasswordOTPResult(false, "Account does not exists.");
			}                

			var otp = _otpService.Generate($"{userAccount.Id}");

			while (otp.ToString().Length < 6)
			{
				otp = _otpService.Generate($"{userAccount.Id}");
			}

			// Load OTP email template
			var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates", "Email", "OtpVerificationEmail.html");
			var emailBody = await File.ReadAllTextAsync(templatePath, cancellationToken);

			// Replace placeholders
			var supportEmail = _configuration["Support:Email"] ?? "support@Aegis.com";
			emailBody = emailBody
				.Replace("{otp}", otp.ToString())
				.Replace("{supportEmail}", supportEmail);

			// Send HTML email with OTP
			await _emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
			{
				Subject = "Your Password Reset Verification Code",
				To = userAccount.Email,
				HtmlBody = emailBody
			});

			_logger.LogInformation("OTP sent successfully to {Email} for user {UserId}", userAccount.Email, userAccount.Id);

			return new SendForgotPasswordOTPResult(true, "OTP sent successfully.");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error sending OTP email: {Message}", ex.Message);
			return new SendForgotPasswordOTPResult(false, "Something went wrong");
		}
	}
}
