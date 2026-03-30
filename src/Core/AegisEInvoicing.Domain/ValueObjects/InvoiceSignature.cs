namespace AegisEInvoicing.Domain.ValueObjects;

public class InvoiceSignature : ValueObject
{
    public string? Id { get; }
    public string? SignatoryParty { get; }
    public string? DigitalSignatureAttachment { get; }

    private InvoiceSignature()
    {
    }

    private InvoiceSignature(string? id, string? signatoryParty, string? digitalSignatureAttachment)
    {
        Id = id;
        SignatoryParty = signatoryParty;
        DigitalSignatureAttachment = digitalSignatureAttachment;
    }

    public static InvoiceSignature Create(string? id = null, string? signatoryParty = null, string? digitalSignatureAttachment = null)
    {
        return new InvoiceSignature(
            id?.Trim(),
            signatoryParty?.Trim(),
            digitalSignatureAttachment?.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id ?? string.Empty;
        yield return SignatoryParty ?? string.Empty;
        yield return DigitalSignatureAttachment ?? string.Empty;
    }
}