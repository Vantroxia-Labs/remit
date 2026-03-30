namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Represents the status of an SFTP user
/// </summary>
public enum SFTPUserStatus
{
    /// <summary>
    /// SFTP user is active and can access the system
    /// </summary>
    Active = 1,

    /// <summary>
    /// SFTP user is inactive and cannot access the system
    /// </summary>
    Inactive = 2
}