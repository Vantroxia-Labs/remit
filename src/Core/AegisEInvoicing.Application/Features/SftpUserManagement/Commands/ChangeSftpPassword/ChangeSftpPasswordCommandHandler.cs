using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ChangeSftpPassword;

/// <summary>
/// Handler for changing SFTP user passwords in database (SFTPGo uses external auth)
/// </summary>
public class ChangeSftpPasswordCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<ChangeSftpPasswordCommandHandler> logger,
    IEmailService emailService,
    IPasswordHasher<SFTPUser> passwordHasher) : IRequestHandler<ChangeSftpPasswordCommand, ChangeSftpPasswordResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<ChangeSftpPasswordCommandHandler> _logger = logger;
    private readonly IEmailService _emailService = emailService;
    private readonly IPasswordHasher<SFTPUser> _passwordHasher = passwordHasher;

    public async Task<ChangeSftpPasswordResult> Handle(ChangeSftpPasswordCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting SFTP password change for username: {Username}", request.Username);

        try
        {
            var sftpUser = await _context.SFTPUsers
                .FirstOrDefaultAsync(s => s.Username == request.Username && !s.IsDeleted, cancellationToken);

            if (sftpUser == null)
            {
                _logger.LogWarning("SFTP user not found: {Username}", request.Username);
                return new ChangeSftpPasswordResult(false, "SFTP user not found");
            }

            // Verify old password if provided (ClientAdmin changing own password)
            if (!string.IsNullOrEmpty(request.OldPassword))
            {
                var verificationResult = _passwordHasher.VerifyHashedPassword(null!, sftpUser.Password, request.OldPassword);
                var isOldPasswordValid = verificationResult != PasswordVerificationResult.Failed;
                if (!isOldPasswordValid)
                {
                    _logger.LogWarning("Invalid old password for user: {Username}", request.Username);
                    return new ChangeSftpPasswordResult(false, "Invalid old password");
                }
            }

            var hashedPassword = _passwordHasher.HashPassword(null!, request.NewPassword);

            // Update password in database
            var currentUserId = _currentUserService.UserId ?? Guid.Empty;
            sftpUser.UpdatePassword(hashedPassword, currentUserId);
            await _context.SaveChangesAsync(cancellationToken);

            // Email the new SFTP password to the business admin/contact
            try
            {
                string? toEmail = null;
                string? adminFirstName = null;

                if (sftpUser.BusinessId.HasValue)
                {
                    var business = await _context.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == sftpUser.BusinessId.Value, cancellationToken);
                    if (business != null)
                    {
                        toEmail = business.ContactEmail;
                        if (business.AdminUserId.HasValue)
                        {
                            var adminUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == business.AdminUserId.Value, cancellationToken);
                            if (adminUser != null)
                            {
                                toEmail = adminUser.Email ?? toEmail;
                                adminFirstName = adminUser.FirstName;
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(toEmail))
                {
                    var emailBody = $@"Dear {adminFirstName ?? request.Username},

Your SFTP password has been changed successfully.

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📁 SFTP ACCESS CREDENTIALS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🖥️  SFTP Username: {request.Username}
🔐 New SFTP Password: {request.NewPassword}

If you did not request this change, please contact support immediately.

Best regards,
EInvoice Integrator Team";

                    await _emailService.SendEmailAsync(new EmailMessage
                    {
                        Subject = "SFTP Password Changed",
                        To = toEmail,
                        TextBody = emailBody
                    }, cancellationToken);
                }
                else
                {
                    _logger.LogWarning("Could not determine email recipient for SFTP user {Username}", request.Username);
                }
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, "Failed to send SFTP password change email for user {Username}", request.Username);
            }

            _logger.LogInformation("Successfully changed password for SFTP user: {Username}", request.Username);
            return new ChangeSftpPasswordResult(true, "Password changed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for SFTP user: {Username}", request.Username);
            return new ChangeSftpPasswordResult(false, $"Error changing password: {ex.Message}");
        }
    }
}