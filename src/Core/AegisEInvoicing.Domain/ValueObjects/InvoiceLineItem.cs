namespace AegisEInvoicing.Domain.ValueObjects;

public class InvoiceLineItem : ValueObject
{
    public string Description { get; } = null!;
    public string Name { get; } = null!;
    public string? SellersItemIdentification { get; }

    private InvoiceLineItem() { }

    private InvoiceLineItem(
        string description,
        string name,
        string? sellersItemIdentification)
    {
        Description = description;
        Name = name;
        SellersItemIdentification = sellersItemIdentification;
    }

    public static InvoiceLineItem Create(
        string description,
        string name,
        string? sellersItemIdentification = null)
    {
        return new InvoiceLineItem(
            description,
            name,
            sellersItemIdentification?.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Description;
        yield return Name;
        yield return SellersItemIdentification ?? string.Empty;
    }
}
