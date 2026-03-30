using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Service for authenticating SFTP virtual users against the database
/// </summary>
public interface IVirtualUserAuthenticationService
{
    Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<SftpUserContext?> GetUserContextAsync(string username, CancellationToken cancellationToken = default);
}

public class VirtualUserAuthenticationService(
    IApplicationDbContext context,
    ILogger<VirtualUserAuthenticationService> logger,
    IPasswordHasher<Domain.Entities.BusinessManagement.SFTPUser> passwordHasher) : IVirtualUserAuthenticationService
{
    private readonly IApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));
    private readonly ILogger<VirtualUserAuthenticationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IPasswordHasher<Domain.Entities.BusinessManagement.SFTPUser> _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));

    public async Task<bool> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var sftpUser = await _context.SFTPUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

            if (sftpUser == null)
            {
                _logger.LogWarning("SFTP authentication failed: User not found - {Username}", username);
                return false;
            }

            if (sftpUser.Status != Domain.Enums.SFTPUserStatus.Active)
            {
                _logger.LogWarning("SFTP authentication failed: User is inactive - {Username}", username);
                return false;
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(null!, sftpUser.Password, password);
            var isPasswordValid = verificationResult != PasswordVerificationResult.Failed;

            if (!isPasswordValid)
            {
                _logger.LogWarning("SFTP authentication failed: Invalid password - {Username}", username);
                return false;
            }

            _logger.LogInformation("SFTP authentication successful - {Username}", username);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during SFTP authentication for user: {Username}", username);
            return false;
        }
    }

    public async Task<SftpUserContext?> GetUserContextAsync(string username, CancellationToken cancellationToken = default)
    {
        try
        {
            var sftpUser = await _context.SFTPUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

            if (sftpUser == null || sftpUser.Status != Domain.Enums.SFTPUserStatus.Active)
            {
                return null;
            }

            return new SftpUserContext
            {
                Username = sftpUser.Username,
                BusinessId = sftpUser.BusinessId,
                RootDirectory = sftpUser.RootDirectoryPath,
                WorkingDirectory = sftpUser.WorkingDirectory,
                IsEnabled = sftpUser.SftpInvoiceTransmissionEnabled
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting SFTP user context for: {Username}", username);
            return null;
        }
    }
}

/// <summary>
/// Context object representing an authenticated SFTP user session
/// </summary>
public class SftpUserContext
{
    public string Username { get; init; } = string.Empty;
    public Guid? BusinessId { get; init; }
    public string RootDirectory { get; init; } = string.Empty;
    public string WorkingDirectory { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
}
