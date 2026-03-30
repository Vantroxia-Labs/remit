namespace AegisEInvoicing.Domain.ValueObjects;

public class InvoiceType : ValueObject
{
    public string Name { get; }
    public int Code { get; }

    private InvoiceType()
    {
        Name = string.Empty;
    }

    private InvoiceType(string name, int code)
    {
        Name = name;
        Code = code;
    }

    public static InvoiceType Create(string name, int code)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Invoice Type name cannot be null or empty", nameof(name));

        if (code < 0)
            throw new ArgumentException("Invoice Type code must be greater than 0", nameof(code));

        return new InvoiceType(
            name,
            code);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Code;
    }
}
