namespace AegisEInvoicing.Domain.Constants;

/// <summary>
/// Constants for system roles used throughout the SaaS platform
/// Role definitions are managed exclusively by Aegis platform administrators
/// Merchants and branches can only assign these predefined roles to their users
/// </summary>
public static class RoleConstants
{
    /// <summary>
    /// Platform Administrator - Aegis admin with full platform access
    /// Can manage all merchants, subscriptions, platform settings, and role definitions
    /// Only Aegis staff can have this role
    /// </summary>
    public const string AegisAdmin = "AegisAdmin";

    /// <summary>
    /// Merchant Administrator - Full control within their merchant organization
    /// Can assign roles to users, manage branches, and settings within their merchant
    /// Cannot create or modify role definitions (managed by Aegis)
    /// </summary>
    public const string ClientAdmin = "ClientAdmin";

    public const string ClientUser = "ClientUser";


    /// <summary>
    /// Gets all available system roles (defined and managed by Aegis)
    /// </summary>
    public static readonly string[] AllRoles = [
        AegisAdmin,
        ClientAdmin,
        ClientUser
    ];

    /// <summary>
    /// Admin roles that have elevated privileges
    /// </summary>
    public static readonly string[] AdminRoles = [
        AegisAdmin,
        ClientAdmin,
        ClientUser
    ];

    /// <summary>
    /// Platform-level roles (Aegis staff only)
    /// These roles can manage role definitions and platform-wide settings
    /// </summary>
    public static readonly string[] PlatformRoles = [
        AegisAdmin
    ];

    /// <summary>
    /// Merchant-level roles (client organization staff)
    /// These roles can only assign existing roles, not create new ones
    /// </summary>
    public static readonly string[] MerchantRoles = [
        ClientAdmin,
        ClientUser
    ];
}