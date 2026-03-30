using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;

/// <summary>
/// Command to create a new user within a business/branch
/// Critical security: Only business/branch admins can create users in their scope
/// </summary>
public record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    //string Password,
    string? PhoneNumber,
    IEnumerable<Guid> RoleIds) : IRequest<CreateUserResult>, ITransactionalCommand;

public record CreateUserResult(
    bool IsSuccess,
    string Message,
    Guid? UserId = null)
{
    public static CreateUserResult Success(Guid userId, string message)
        => new(true, message, userId);
        
    public static CreateUserResult Failure(string message)
        => new(false, message);
}

public class CreateUserCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser, IEmailService emailService, ILogger<CreateUserCommandHandler> logger) : IRequestHandler<CreateUserCommand, CreateUserResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<CreateUserCommandHandler> _logger = logger;

    public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Step 1: Security validation - ensure user is authenticated and has business context
            if (!_currentUser.IsAuthenticated || _currentUser.BusinessId == null)
            {
                return CreateUserResult.Failure("Authentication required");
            }

            // Step 3: Get the business and verify admin relationship
            var business = await _context.Businesses
                .FirstOrDefaultAsync(m => m.Id == _currentUser.BusinessId.Value, cancellationToken);

            if (business == null)
            {
                return CreateUserResult.Failure("Business not found");
            }

            // Step 4: CRITICAL SECURITY CHECK - Ensure current user can manage users
            if (!business.CanManageUsers(_currentUser.UserId!.Value, _currentUser.BranchId))
            {
                return CreateUserResult.Failure("Insufficient permissions to create users in this scope");
            }

            // Step 5: Validate that email is not already in use
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, 
                cancellationToken);

            if (existingUser != null)
            {
                return CreateUserResult.Failure($"User with email '{request.Email}' already exists");
            }

            // Step 6: Validate platform roles exist
            var platformRoles = await _context.PlatformRoles
                .Where(r => request.RoleIds.Contains(r.Id) && r.IsActive)
                .ToListAsync(cancellationToken);

            if (platformRoles.Count != request.RoleIds.Count())
            {
                return CreateUserResult.Failure("One or more roles not found or are inactive");
            }

            // Step 7: Create password hash
            var tempPassword = GenerateSecurePassword();
            var passwordHash = PasswordHash.Create(tempPassword);

            // Step 8: Create the user
            var user = User.Create(
                _currentUser.BusinessId.Value,
                request.FirstName,
                request.LastName,
                request.Email,
                passwordHash,
                _currentUser.UserId.Value,
                _currentUser.BranchId,
                request.PhoneNumber);

            // Step 9: Assign platform roles to user
            foreach (var platformRole in platformRoles)
            {
                user.AssignRole(platformRole.Id, _currentUser.UserId.Value);
            }

            // Step 10: Save user
            await _context.Users.AddAsync(user, cancellationToken);
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

            return CreateUserResult.Success(
                user.Id,
                $"User '{request.Email}' created successfully in business '{business.Name}'");
        }
        catch (Exception ex)
        {
            return CreateUserResult.Failure($"Failed to create user: {ex.Message}");
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