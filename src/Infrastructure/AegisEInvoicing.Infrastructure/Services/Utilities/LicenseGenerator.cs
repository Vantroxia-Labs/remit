using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.Utilitties;

public class LicenseGenerator
{
    private readonly string _privateKey;
    private readonly string _publicKey;

    public LicenseGenerator()
    {
        // In production, these keys should be stored securely
        // This is for demonstration purposes
        using var rsa = RSA.Create(2048);
        _privateKey = Convert.ToBase64String(rsa.ExportRSAPrivateKey());
        _publicKey = Convert.ToBase64String(rsa.ExportRSAPublicKey());
    }

    public LicenseGenerator(string privateKey, string publicKey)
    {
        _privateKey = privateKey;
        _publicKey = publicKey;
    }

    public OfflineLicense GenerateLicense(
        string organizationName,
        string organizationId,
        DateTimeOffset expiryDate,
        string? hardwareFingerprint = null,
        Dictionary<string, object>? features = null)
    {
        var licenseData = new LicenseData
        {
            LicenseId = Guid.CreateVersion7().ToString(),
            OrganizationName = organizationName,
            OrganizationId = organizationId,
            ExpiryDate = expiryDate,
            IssueDate = DateTimeOffset.UtcNow,
            HardwareFingerprint = hardwareFingerprint,
            Features = features ?? new Dictionary<string, object>
            {
                ["MaxUsers"] = 100,
                ["MaxBranches"] = 10,
                ["MaxInvoicesPerMonth"] = 10000,
                ["AllowAPIAccess"] = true
            }
        };

        // Create signature
        var dataToSign = SerializeLicenseData(licenseData);
        var signature = SignData(dataToSign);

        // Create license key
        var licenseKey = CreateLicenseKey(licenseData, signature);

        // Create license file content
        var licenseFile = new OfflineLicense
        {
            LicenseKey = licenseKey,
            LicenseData = licenseData,
            Signature = signature,
            PublicKey = _publicKey
        };

        return licenseFile;
    }

    public bool ValidateLicense(OfflineLicense license)
    {
        try
        {
            // Check expiry
            if (license.LicenseData.ExpiryDate <= DateTimeOffset.UtcNow)
                return false;

            // Verify signature
            var dataToVerify = SerializeLicenseData(license.LicenseData);
            return VerifySignature(dataToVerify, license.Signature, license.PublicKey);
        }
        catch
        {
            return false;
        }
    }

    private static string CreateLicenseKey(LicenseData data, string signature)
    {
        var keyData = new
        {
            OrgId = data.OrganizationId,
            Exp = data.ExpiryDate.ToUnixTimeSeconds(),
            HW = data.HardwareFingerprint ?? "",
            Sig = signature[..32] // Use part of signature for validation
        };

        var json = JsonSerializer.Serialize(keyData);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private static string SerializeLicenseData(LicenseData data)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        return JsonSerializer.Serialize(data, options);
    }

    private string SignData(string data)
    {
        using var rsa = RSA.Create();
        rsa.ImportRSAPrivateKey(Convert.FromBase64String(_privateKey), out _);
        
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signature = rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        
        return Convert.ToBase64String(signature);
    }

    private static bool VerifySignature(string data, string signature, string publicKey)
    {
        try
        {
            using var rsa = RSA.Create();
            rsa.ImportRSAPublicKey(Convert.FromBase64String(publicKey), out _);
            
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signatureBytes = Convert.FromBase64String(signature);
            
            return rsa.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch
        {
            return false;
        }
    }

    public static string GenerateHardwareFingerprint()
    {
        var machineName = Environment.MachineName;
        var processorCount = Environment.ProcessorCount;
        var osVersion = Environment.OSVersion.ToString();
        var userName = Environment.UserName;
        
        var fingerprint = $"{machineName}|{processorCount}|{osVersion}|{userName}";
        
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(fingerprint));
        return Convert.ToBase64String(bytes);
    }
}

public class OfflineLicense
{
    public string LicenseKey { get; set; } = string.Empty;
    public LicenseData LicenseData { get; set; } = new();
    public string Signature { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;

    public string ToJson()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };
        return JsonSerializer.Serialize(this, options);
    }

    public static OfflineLicense? FromJson(string json)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            return JsonSerializer.Deserialize<OfflineLicense>(json, options);
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveToFileAsync(string filePath)
    {
        var json = ToJson();
        await File.WriteAllTextAsync(filePath, json);
    }

    public static async Task<OfflineLicense?> LoadFromFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var json = await File.ReadAllTextAsync(filePath);
        return FromJson(json);
    }
}

public class LicenseData
{
    public string LicenseId { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public DateTimeOffset IssueDate { get; set; }
    public DateTimeOffset ExpiryDate { get; set; }
    public string? HardwareFingerprint { get; set; }
    public Dictionary<string, object> Features { get; set; } = [];
}