namespace AegisEInvoicing.Infrastructure.Services.Session;

/// <summary>
/// Session management configuration settings
/// Addresses VAPT finding: Concurrent login enabled
/// </summary>
public sealed record SessionManagementSettings
{
    public const string SectionName = "SessionManagement";

    /// <summary>
    /// Maximum number of concurrent sessions allowed per user
    /// SECURITY: Set to 1 to prevent concurrent logins (VAPT requirement)
    /// Default: 1 (single session only - addresses VAPT finding)
    /// </summary>
    public int MaxConcurrentSessions { get; set; } = 1;

    /// <summary>
    /// Session timeout in minutes
    /// Session expires after this period of inactivity
    /// Default: 30 minutes
    /// </summary>
    public int SessionTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Whether to enforce the session limit (terminate oldest sessions when limit exceeded)
    /// SECURITY: Must be true to prevent concurrent sessions (VAPT requirement)
    /// Default: true (enforcement enabled)
    /// </summary>
    public bool EnforceSessionLimit { get; set; } = true;
}
