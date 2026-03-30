namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data like API keys and secrets.
/// Uses industry-standard encryption algorithms for secure data protection.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain text string
    /// </summary>
    /// <param name="plainText">The text to encrypt</param>
    /// <returns>Base64-encoded encrypted string</returns>
    Task<string> EncryptAsync(string plainText);

    /// <summary>
    /// Decrypts an encrypted string
    /// </summary>
    /// <param name="encryptedText">Base64-encoded encrypted string</param>
    /// <returns>Decrypted plain text</returns>
    Task<string> DecryptAsync(string encryptedText);

    /// <summary>
    /// Generates a new encryption key (for key rotation)
    /// </summary>
    /// <returns>Base64-encoded encryption key</returns>
    string GenerateKey();

    /// <summary>
    /// Validates if an encrypted string can be decrypted
    /// </summary>
    /// <param name="encryptedText">Base64-encoded encrypted string</param>
    /// <returns>True if the string can be decrypted</returns>
    Task<bool> ValidateEncryptedDataAsync(string encryptedText);
}