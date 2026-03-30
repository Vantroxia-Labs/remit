namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Aegis-specific roles for platform administration and operations
/// These roles are in addition to the standard platform roles and provide
/// fine-grained access control for different Aegis user functions
/// </summary>
public enum AegisRole
{
    AegisAdmin = 1
}

/// <summary>
/// Extension methods for AegisRole enum
/// </summary>
public static class AegisRoleExtensions
{
    /// <summary>
    /// Get the display name for the Aegis role
    /// </summary>
    public static string GetDisplayName(this AegisRole role)
    {
        return role switch
        {
            AegisRole.AegisAdmin => "System Administrator",
            _ => role.ToString()
        };
    }

    /// <summary>
    /// Get the description for the Aegis role
    /// </summary>
    public static string GetDescription(this AegisRole role)
    {
        return role switch
        {
            AegisRole.AegisAdmin => "Full system access including user management, business operations, and system configuration",
            _ => "Aegis platform role"
        };
    }

    /// <summary>
    /// Check if the role has administrative privileges
    /// </summary>
    public static bool IsAdminRole(this AegisRole role)
    {
        return role is AegisRole.AegisAdmin;
    }

    /// <summary>
    /// Check if the role has business management capabilities
    /// </summary>
    public static bool CanManageBusinesses(this AegisRole role)
    {
        return role is AegisRole.AegisAdmin;
    }

    /// <summary>
    /// Check if the role has reporting capabilities
    /// </summary>
    public static bool CanGenerateReports(this AegisRole role)
    {
        return role is AegisRole.AegisAdmin;
    }
  

    /// <summary>
    /// Get all available Aegis roles
    /// </summary>
    public static AegisRole[] GetAllRoles()
    {
        return [
            AegisRole.AegisAdmin
        ];
    }
}