using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.RequestChangePassword;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.SendForgotPasswordOTP;

public class ForgotPasswordCommandHandler(IApplicationDbContext context, IEmailService emailService, ITotpService otpService, ILogger<SendForgotPasswordOTPCommandHandler> logger) : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<SendForgotPasswordOTPCommandHandler> _logger = logger;
    private readonly ITotpService _otpService = otpService;

    public async Task<ForgotPasswordResult> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userAccount = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.phone_email, cancellationToken);

            if (userAccount is null)
            {
                userAccount = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.phone_email, cancellationToken);

                if (userAccount is null)
                    return new ForgotPasswordResult(false, "Account does not exists.");
            }

            if (!_otpService.Verify(Convert.ToInt32(request.otp), $"{userAccount.Id}"))
                return new ForgotPasswordResult(false, "Invalid otp provided.");

            var passwordHash = PasswordHash.Create(request.password);

            userAccount.ChangePassword(passwordHash, isReset: true);
            await _context.SaveChangesAsync(cancellationToken);

            return new ForgotPasswordResult(true, "Password changed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new ForgotPasswordResult(false, "Something went wrong");
        }
    }
}
