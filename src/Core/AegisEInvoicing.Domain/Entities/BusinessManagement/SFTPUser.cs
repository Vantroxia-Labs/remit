using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Enums;
using System.Security.Cryptography;
using System.Text;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

/// <summary>
/// Represents an SFTP user in the SFTPGo FTP server system
/// </summary>
public class SFTPUser : AuditableEntity
{
    /// <summary>
    /// The business this SFTP user belongs to
    /// </summary>
    public Guid? BusinessId { get; private set; }

    /// <summary>
    /// The SFTPGo SFTP username (typically the business name)
    /// </summary>
    public string Username { get; private set; } = null!;

    /// <summary>
    /// The SFTP password (stored as plain text for SFTP client compatibility)
    /// Recommend enabling database-level encryption at rest (TDE/Always Encrypted)
    /// </summary>
    public string Password { get; private set; } = null!;

    /// <summary>
    /// Status of the SFTP user (Active/Inactive)
    /// </summary>
    public SFTPUserStatus Status { get; private set; } = SFTPUserStatus.Active;

    /// <summary>
    /// The root directory path for this SFTP user
    /// </summary>
    public string RootDirectoryPath { get; private set; } = null!;

    /// <summary>
    /// The working directory path for this SFTP user (e.g., /{username}/)
    /// </summary>
    public string WorkingDirectory { get; private set; } = null!;

    /// <summary>
    /// Whether the user directories have been created on the file system
    /// </summary>
    public bool DirectoriesCreated { get; private set; } = false;

    /// <summary>
    /// Toggle to enable or disable SFTP invoice transmission for this user
    /// </summary>
    public bool SftpInvoiceTransmissionEnabled { get; private set; } = false;

    /// <summary>
    /// Date when the SFTP user was created in SFTPGo
    /// </summary>
    public DateTimeOffset? SFTPGoCreatedAt { get; private set; }

    /// <summary>
    /// Date when the SFTP user was last synced with SFTPGo
    /// </summary>
    public DateTimeOffset? LastSyncedAt { get; private set; }

    /// <summary>
    /// Navigation property to the associated business
    /// </summary>
    public Business? Business { get; private set; }

    private SFTPUser() { } // EF Constructor

    /// <summary>
    /// Creates a new SFTP user entity
    /// </summary>
    public static SFTPUser Create(
        Guid businessId,
        string username,
        string password,
        string rootDirectoryPath,
        string workingDirectory,
        Guid createdBy)
    {
        ValidateInputs(username, password, rootDirectoryPath, workingDirectory);

        return new SFTPUser
        {
            BusinessId = businessId,
            Username = username,
            Password = password,
            RootDirectoryPath = rootDirectoryPath,
            WorkingDirectory = workingDirectory,
            Status = SFTPUserStatus.Active,
            DirectoriesCreated = false,
            SftpInvoiceTransmissionEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };
    }

    /// <summary>
    /// Updates the password for the SFTP user
    /// </summary>
    public void UpdatePassword(string newPassword, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("Password cannot be null or empty", nameof(newPassword));

        Password = newPassword;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the user as active
    /// </summary>
    public void Activate(Guid updatedBy)
    {
        if (Status == SFTPUserStatus.Active)
            return;

        Status = SFTPUserStatus.Active;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the user as inactive
    /// </summary>
    public void Deactivate(Guid updatedBy)
    {
        if (Status == SFTPUserStatus.Inactive)
            return;

        Status = SFTPUserStatus.Inactive;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the directories as created
    /// </summary>
    public void MarkDirectoriesAsCreated(Guid updatedBy)
    {
        DirectoriesCreated = true;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the SFTPGo creation timestamp
    /// </summary>
    public void SetSFTPGoCreatedAt(DateTimeOffset createdAt, Guid updatedBy)
    {
        SFTPGoCreatedAt = createdAt;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the last sync timestamp
    /// </summary>
    public void UpdateLastSyncedAt(Guid updatedBy)
    {
        LastSyncedAt = DateTimeOffset.UtcNow;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the root directory path
    /// </summary>
    public void UpdateRootDirectoryPath(string newRootPath, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newRootPath))
            throw new ArgumentException("Root directory path cannot be null or empty", nameof(newRootPath));

        RootDirectoryPath = newRootPath;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Checks if the SFTP user is active
    /// </summary>
    public bool IsActive() => Status == SFTPUserStatus.Active;

    /// <summary>
    /// Gets the expected directory structure for this SFTP user
    /// </summary>
    public IEnumerable<string> GetExpectedDirectories()
    {
        var basePath = RootDirectoryPath;
        return new[]
        {
            Path.Combine(basePath, "PROCESSED"),
            Path.Combine(basePath, "NACK"),
            Path.Combine(basePath, "ACK")
        };
    }

    /// <summary>
    /// Updates the working directory path
    /// </summary>
    public void UpdateWorkingDirectory(string newWorkingDirectory, Guid updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newWorkingDirectory))
            throw new ArgumentException("Working directory cannot be null or empty", nameof(newWorkingDirectory));

        WorkingDirectory = newWorkingDirectory;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Decrypts the password for use (not recommended for production use)
    /// </summary>
    public string GetDecryptedPassword()
    {
        // Since the password is stored in plain text per requirements, just return it
        return Password;
    }

    // Encryption is disabled per current requirements; method retained for backward-compatibility if needed
    private static string EncryptPassword(string password)
    {
        // For production, use a proper key management system
        // This is a simplified implementation for demonstration
        const string key = "SFTPUser2024Key!"; // In production, store this securely
        
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.GenerateIV();
        
        using var encryptor = aes.CreateEncryptor();
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var encryptedBytes = encryptor.TransformFinalBlock(passwordBytes, 0, passwordBytes.Length);
        
        // Combine IV and encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);
        
        return Convert.ToBase64String(result);
    }

    // Decryption is disabled per current requirements; method retained for backward-compatibility if needed
    private static string DecryptPassword(string encryptedPassword)
    {
        const string key = "SFTPUser2024Key!"; // In production, store this securely
        
        var fullCipher = Convert.FromBase64String(encryptedPassword);
        
        using var aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        
        // Extract IV
        var iv = new byte[aes.IV.Length];
        var cipher = new byte[fullCipher.Length - iv.Length];
        
        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);
        
        aes.IV = iv;
        
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    public void EnableInvoiceTransmission(Guid updatedBy)
    {
        if (!SftpInvoiceTransmissionEnabled)
        {
            SftpInvoiceTransmissionEnabled = true;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    public void DisableInvoiceTransmission(Guid updatedBy)
    {
        if (SftpInvoiceTransmissionEnabled)
        {
            SftpInvoiceTransmissionEnabled = false;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTimeOffset.UtcNow;
        }
    }

    private static void ValidateInputs(string username, string password, string rootDirectoryPath, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or empty", nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        if (string.IsNullOrWhiteSpace(rootDirectoryPath))
            throw new ArgumentException("Root directory path cannot be null or empty", nameof(rootDirectoryPath));

        if (string.IsNullOrWhiteSpace(workingDirectory))
            throw new ArgumentException("Working directory cannot be null or empty", nameof(workingDirectory));

        if (username.Length > 100)
            throw new ArgumentException("Username cannot exceed 100 characters", nameof(username));

        if (password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters long", nameof(password));

        if (rootDirectoryPath.Length > 500)
            throw new ArgumentException("Root directory path cannot exceed 500 characters", nameof(rootDirectoryPath));

        if (workingDirectory.Length > 200)
            throw new ArgumentException("Working directory cannot exceed 200 characters", nameof(workingDirectory));

        // Validate username contains only allowed characters
        if (username.Any(c => !char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != '.'))
            throw new ArgumentException("Username can only contain letters, digits, underscore, hyphen, and dot", nameof(username));
    }
}