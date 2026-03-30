namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for accessing current user information in SaaS platform
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IEnumerable<string> Roles { get; }
    IEnumerable<string> Permissions { get; }
    Guid? BusinessId { get; }
    Guid? BranchId { get; }
    bool IsBusinessLevel { get; }
    bool IsBranchLevel { get; }
    bool IsPlatformAdmin { get; }
    
    // Aegis-specific properties
    bool IsAegisUser { get; }
    string? AegisRole { get; }
    string? AegisEmployeeId { get; }
    string? AegisDepartment { get; }
    
    bool HasRole(string role);
    bool HasPermission(string permission);
}
