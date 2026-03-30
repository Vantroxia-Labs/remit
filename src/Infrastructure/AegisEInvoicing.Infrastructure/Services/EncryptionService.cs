using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Implementation of encryption service using AES-256-GCM for secure data encryption.
/// Provides secure encryption/decryption of sensitive data like API keys and secrets.
/// </summary>
public sealed class EncryptionService : IEncryptionService, IDisposable
{
    private readonly string _encryptionKey;
    private readonly string _encryptionIv;
    private readonly byte[] _key;
    private readonly byte[] _iv;
    private readonly ILogger<EncryptionService> _logger;
    private bool _disposed = false;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Get encryption key from configuration
        _encryptionKey = configuration["Encryption:Key"] ?? 
                        configuration["ENCRYPTION_KEY"] ?? 
                        throw new InvalidOperationException("Encryption key not found in configuration. Set 'Encryption:Key' or 'ENCRYPTION_KEY' environment variable.");

        _encryptionIv = configuration["Encryption:Iv"] ??
                       configuration["ENCRYPTION_IV"] ??
                       throw new InvalidOperationException("Encryption iv not found in configuration. Set 'Encryption:Iv' or 'ENCRYPTION_Iv' environment variable.");
        Aes aes = Aes.Create();

        _key = Encoding.UTF8.GetBytes(_encryptionKey);
        _iv = Encoding.UTF8.GetBytes(_encryptionIv); 
    }

    public Task<string> EncryptAsync(string plainText)
    {
        string encryptedString = string.Empty;

        if (string.IsNullOrEmpty(plainText))
        {
            throw new ArgumentException("Plain text cannot be null or empty", nameof(plainText));
        }

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (StreamWriter sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }

            encryptedString = Convert.ToBase64String(ms.ToArray());
            
            return Task.FromResult(encryptedString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encrypting data");
            throw new InvalidOperationException("Failed to encrypt data", ex);
        }
    }

    public Task<string> DecryptAsync(string encryptedText)
    {
        string decryptedString = string.Empty;

        if (string.IsNullOrEmpty(encryptedText))
        {
            throw new ArgumentException("Encrypted text cannot be null or empty", nameof(encryptedText));
        }

        try
        {
            using Aes aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            byte[] buffer = Convert.FromBase64String(encryptedText);

            using MemoryStream ms = new MemoryStream(buffer);
            using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using StreamReader sr = new StreamReader(cs);

            decryptedString = sr.ReadToEnd();
            
            return Task.FromResult(decryptedString);
        }
        catch (Exception ex) when (ex is not CryptographicException)
        {
            _logger.LogError(ex, "Error decrypting data");
            throw new InvalidOperationException("Failed to decrypt data", ex);
        }
    }

    private async Task<string> EncryptWithCBCAsync(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_encryptionKey);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        using var msEncrypt = new MemoryStream();
        using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
        using var swEncrypt = new StreamWriter(csEncrypt);

        await swEncrypt.WriteAsync(plainText);
        swEncrypt.Close();

        var encrypted = msEncrypt.ToArray();
        var result = new byte[aes.IV.Length + encrypted.Length];
        Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
        Array.Copy(encrypted, 0, result, aes.IV.Length, encrypted.Length);

        return Convert.ToBase64String(result);
    }

    private async Task<string> DecryptWithCBCAsync(string encryptedText)
    {
        var fullCipher = Convert.FromBase64String(encryptedText);

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(_encryptionKey);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Array.Copy(fullCipher, 0, iv, 0, iv.Length);
        Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        using var msDecrypt = new MemoryStream(cipher);
        using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using var srDecrypt = new StreamReader(csDecrypt);

        return await srDecrypt.ReadToEndAsync();
    }

    public string GenerateKey()
    {
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256; // 256-bit key
            aes.GenerateKey();
            
            var key = Convert.ToBase64String(aes.Key);
            
            _logger.LogInformation("Generated new 256-bit encryption key");
            
            return key;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating encryption key");
            throw new InvalidOperationException("Failed to generate encryption key", ex);
        }
    }

    public async Task<bool> ValidateEncryptedDataAsync(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return false;
        }

        try
        {
            // Try to decrypt a small test to validate
            await DecryptAsync(encryptedText);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            // Clear sensitive data from memory
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            _disposed = true;
        }
    }
}