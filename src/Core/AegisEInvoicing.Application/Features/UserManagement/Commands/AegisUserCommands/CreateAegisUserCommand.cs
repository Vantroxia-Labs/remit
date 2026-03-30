using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.OnboardBusiness;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.AegisUserCommands;

/// <summary>
/// Command to create a new Aegis user (platform administrators only)
/// Critical security: Only Aegis platform admins can create other Aegis users
/// Aegis users are NOT tied to any business
/// </summary>
public record CreateAegisUserCommand(
    string FirstName,
    string LastName,
    string Email,
    //string Password,
    AegisRole AegisRole,
    string? PhoneNumber,
    string? AegisEmployeeId,
    string? AegisDepartment) : IRequest<CreateAegisUserResult>, ITransactionalCommand;

public record CreateAegisUserResult(
    bool IsSuccess,
    string Message,
    Guid? UserId = null)
{
    public static CreateAegisUserResult Success(Guid userId, string message)
        => new(true, message, userId);
        
    public static CreateAegisUserResult Failure(string message)
        => new(false, message);
}

public class CreateAegisUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser, IEmailService emailService, ILogger<CreateAegisUserCommandHandler> logger) : IRequestHandler<CreateAegisUserCommand, CreateAegisUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<CreateAegisUserCommandHandler> _logger = logger;

    public async Task<CreateAegisUserResult> Handle(CreateAegisUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated
            if (!_currentUser.IsAuthenticated || _currentUser.UserId == null)
            {
                return CreateAegisUserResult.Failure("Authentication required");
            }

            // Step 2: Security validation - verify user is Aegis platform admin
            if (!_currentUser.IsAegisUser || !_currentUser.HasRole(RoleConstants.AegisAdmin))
            {
                return CreateAegisUserResult.Failure("Only Aegis Platform Admins can create Aegis users");
            }

            // Step 3: Verify current user exists and is active
            var currentAegisUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value && u.IsAegisUser, cancellationToken);

            if (currentAegisUser == null || !currentAegisUser.CanLogin())
            {
                return CreateAegisUserResult.Failure("Current user is not authorized or is inactive");
            }

            // Step 4: Validate that email is not already in use
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (existingUser != null)
            {
                return CreateAegisUserResult.Failure($"User with email '{request.Email}' already exists");
            }

            // Step 5: Validate Aegis Employee ID is unique if provided
            if (!string.IsNullOrWhiteSpace(request.AegisEmployeeId))
            {
                var existingEmployeeId = await _context.Users
                    .FirstOrDefaultAsync(u => u.AegisEmployeeId == request.AegisEmployeeId, cancellationToken);

                if (existingEmployeeId != null)
                {
                    return CreateAegisUserResult.Failure($"Aegis Employee ID '{request.AegisEmployeeId}' already exists");
                }
            }

            // Step 6: Create temp and password hash
            var tempPassword = GenerateSecurePassword();
            var passwordHash = PasswordHash.Create(tempPassword);

            // Step 7: Create the Aegis user (NOT tied to any business)
            var AegisUser = User.CreateAegisUser(
                request.FirstName,
                request.LastName,
                request.Email,
                passwordHash,
                request.AegisRole,
                _currentUser.UserId.Value,
                request.PhoneNumber,
                request.AegisEmployeeId,
                request.AegisDepartment);

            // Step 8: Save Aegis user
            await _context.Users.AddAsync(AegisUser, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                await _emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
                {
                    Subject = "Account Created!!!",
                    To = request.Email,
                    CcAddresses = ["jc@xtradot.ng"],
                    TextBody = $"Login Email: {request.Email}, Password = {tempPassword}"
                });
            }
            catch (Exception emailEx)
            {
                _logger.LogError(emailEx, emailEx.Message);
            }

            return CreateAegisUserResult.Success(
                AegisUser.Id,
                $"Aegis user '{request.Email}' created successfully with role '{request.AegisRole.GetDisplayName()}'");
        }
        catch (Exception ex)
        {
            return CreateAegisUserResult.Failure($"Failed to create Aegis user: {ex.Message}");
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
}