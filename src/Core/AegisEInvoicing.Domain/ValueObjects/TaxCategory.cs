namespace AegisEInvoicing.Domain.ValueObjects;

public class TaxCategory : ValueObject
{
    public string Name { get; }
    public decimal Percent { get; }

    private TaxCategory()
    {
        Name = string.Empty;
    }

    private TaxCategory(string name, decimal percent)
    {
        Name = name;
        Percent = percent;
    }

    public static TaxCategory Create(string name, decimal percent)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tax category ID cannot be null or empty", nameof(name));

        if (percent < 0 || percent > 100)
            throw new ArgumentException("Tax percent must be between 0 and 100", nameof(percent));

        return new TaxCategory(
            name.Trim(),
            percent);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Name;
        yield return Percent;
    }
}