namespace AegisEInvoicing.Domain.ValueObjects;

public class QRCode : ValueObject
{
    public string EncryptedData { get; }
    public byte[]? Base64Image { get; }
    public DateTimeOffset GeneratedAt { get; }

    // Parameterless constructor for Entity Framework
    private QRCode()
    {
        EncryptedData = string.Empty;
        Base64Image = null;
        GeneratedAt = DateTimeOffset.UtcNow;
    }

    private QRCode(string encryptedData, byte[]? base64Image, DateTimeOffset generatedAt)
    {
        EncryptedData = encryptedData;
        Base64Image = base64Image;
        GeneratedAt = generatedAt;
    }

    public static QRCode Create(string encryptedData, byte[]? base64Image = null)
    {
        if (string.IsNullOrWhiteSpace(encryptedData))
            throw new ArgumentException("Encrypted data cannot be null or empty", nameof(encryptedData));

      return new QRCode(encryptedData, base64Image, DateTimeOffset.UtcNow);
    }

    public string GetBase64String()
    {
        if (string.IsNullOrEmpty(EncryptedData))
            return string.Empty;

        return EncryptedData;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return EncryptedData;
    }
}