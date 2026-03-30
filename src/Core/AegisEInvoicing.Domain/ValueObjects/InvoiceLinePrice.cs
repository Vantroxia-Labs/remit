namespace AegisEInvoicing.Domain.ValueObjects;

public class InvoiceLinePrice : ValueObject
{
    public double PriceAmount { get; }
    public int BaseQuantity { get; }
    public string PriceUnit { get; } = null!;

    private InvoiceLinePrice() { }

    private InvoiceLinePrice(
        double priceAmount,
        int baseQuantity,
        string priceUnit )
    {
        PriceAmount = priceAmount;
        BaseQuantity = baseQuantity;
        PriceUnit = priceUnit;
    }

    public static InvoiceLinePrice Create(
        double priceAmount,
        int baseQuantity,
        string priceUnit)
    {
        if (priceAmount < 0)
            throw new ArgumentException("Price amount cannot be negative", nameof(priceAmount));

        if (baseQuantity < 0)
            throw new ArgumentException("Base quantity cannot be negative", nameof(priceAmount));

        if (string.IsNullOrWhiteSpace(priceUnit))
            throw new ArgumentException("Price unit cannot be null or empty", nameof(priceUnit));

        return new InvoiceLinePrice(
            priceAmount,
            baseQuantity,
            priceUnit);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return PriceAmount;
        yield return BaseQuantity;
        yield return PriceUnit;
    }
}