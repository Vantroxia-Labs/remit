using QRCoder;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AegisEInvoicing.Application.Services;

public static class InvoiceQrService
{
    private const int Size = 300;

    public static string GenerateQrCode(
        string irn,
        string certificate,
        string publicKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(irn);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificate);
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKey);

        // Generate Unix timestamp
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        // Create IRN with timestamp appended (format: "IRN.timestamp")
        var irnWithTimestamp = $"{irn}.{unixTimestamp}";

        // Create payload matching FIRS specification exactly - only irn and certificate
        var payload = new
        {
            irn = irnWithTimestamp,
            certificate = certificate
        };

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        // Encrypt the payload using RSA with the public key
        var encryptedData = EncryptWithPublicKey(payloadJson, publicKey);
        var encryptedBase64 = Convert.ToBase64String(encryptedData);

        // Generate QR code from encrypted data
        var qrCodeBuffer = GenerateQrCodeImage(encryptedBase64, Size);
        var base64string = Convert.ToBase64String(qrCodeBuffer);

        return base64string;

    }

    private static byte[] EncryptWithPublicKey(string data, string publicKeyBase64)
    {
        try
        {
            // Decode the base64 public key
            var publicKeyBytes = Convert.FromBase64String(publicKeyBase64);
            var publicKeyString = Encoding.UTF8.GetString(publicKeyBytes);

            // Remove PEM headers/footers if present
            publicKeyString = publicKeyString
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Trim();

            var keyBytes = Convert.FromBase64String(publicKeyString);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);

            // Use RSA-PKCS1 padding as required by FIRS (not OAEP)
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var encryptedBytes = rsa.Encrypt(dataBytes, RSAEncryptionPadding.Pkcs1);

            return encryptedBytes;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to encrypt data with public key: {ex.Message}", ex);
        }
    }


    private static byte[] GenerateQrCodeImage(string data, int size)
    {
        using var qrGenerator = new QRCoder.QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(data, QRCoder.QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);

        // Convert size to pixels per module (approximate)
        var pixelsPerModule = Math.Max(1, size / 25);

        var qrCodeBytes = qrCode.GetGraphic(pixelsPerModule);
        return qrCodeBytes;
    }
}

public readonly record struct InvoicePayload(string Irn, string Certificate);

public sealed record EncryptedQrResult(string Base64Encrypted, byte[] QrCodePng);
