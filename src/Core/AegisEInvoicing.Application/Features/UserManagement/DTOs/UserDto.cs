namespace AegisEInvoicing.Application.Features.UserManagement.DTOs;

public record UserDto(
    Guid Id,
    Guid? BusinessId,
    Guid? BranchId,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Status,
    bool IsEmailVerified,
    DateTimeOffset? LastLoginAt,
    bool MustChangePassword,
    int FailedLoginAttempts,
    DateTimeOffset? LockedOutUntil,
    IEnumerable<UserRoleDto> Roles,
    UserPreferencesDto Preferences,
    string? BusinessName,
    string? BranchName,
    bool IsBusinessLevel,
    bool IsBranchLevel,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record CreateUserDto(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string Password,
    IEnumerable<Guid> RoleIds);

public record UpdateUserDto(
    string? FirstName,
    string? LastName,
    string? Email,
    string? PhoneNumber);

public record UserRoleDto(
    Guid RoleAssignmentId,
    Guid PlatformRoleId,
    string RoleName,
    string RoleDescription,
    string RoleCategory,
    DateTimeOffset AssignedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? RevocationReason,
    bool IsActive);

public record UserPreferencesDto(
    string Language,
    string TimeZone,
    string DateFormat,
    string NumberFormat,
    bool EmailNotifications,
    bool SmsNotifications,
    bool InvoiceReminders,
    bool SecurityAlerts,
    string Theme,
    int PageSize,
    bool TwoFactorEnabled);