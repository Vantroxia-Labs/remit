namespace AegisEInvoicing.Domain.ValueObjects;

public class DigitalSignature : ValueObject
{
    public string SignatureValue { get; }
    public string Algorithm { get; }
    public string CertificateThumbprint { get; }
    public DateTimeOffset SignedAt { get; }
    public string SignatureFormat { get; } // JAdES, XAdES, etc.

    // Parameterless constructor for Entity Framework
    private DigitalSignature()
    {
        SignatureValue = string.Empty;
        Algorithm = string.Empty;
        CertificateThumbprint = string.Empty;
        SignatureFormat = string.Empty;
        SignedAt = DateTimeOffset.UtcNow;
    }

    private DigitalSignature(
        string signatureValue, 
        string algorithm, 
        string certificateThumbprint, 
        DateTimeOffset signedAt,
        string signatureFormat)
    {
        SignatureValue = signatureValue;
        Algorithm = algorithm;
        CertificateThumbprint = certificateThumbprint;
        SignedAt = signedAt;
        SignatureFormat = signatureFormat;
    }

    public static DigitalSignature Create(
        string signatureValue,
        string algorithm,
        string certificateThumbprint,
        string signatureFormat = "JAdES")
    {
        if (string.IsNullOrWhiteSpace(signatureValue))
            throw new ArgumentException("Signature value cannot be null or empty", nameof(signatureValue));

        if (string.IsNullOrWhiteSpace(algorithm))
            throw new ArgumentException("Algorithm cannot be null or empty", nameof(algorithm));

        if (string.IsNullOrWhiteSpace(certificateThumbprint))
            throw new ArgumentException("Certificate thumbprint cannot be null or empty", nameof(certificateThumbprint));

        if (string.IsNullOrWhiteSpace(signatureFormat))
            throw new ArgumentException("Signature format cannot be null or empty", nameof(signatureFormat));

        return new DigitalSignature(
            signatureValue,
            algorithm,
            certificateThumbprint,
            DateTimeOffset.UtcNow,
            signatureFormat);
    }

    public bool IsExpired(TimeSpan validityPeriod)
    {
        return DateTimeOffset.UtcNow > SignedAt.Add(validityPeriod);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return SignatureValue;
        yield return Algorithm;
        yield return CertificateThumbprint;
        yield return SignedAt;
        yield return SignatureFormat;
    }
}