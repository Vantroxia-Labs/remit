namespace AegisEInvoicing.Domain.ValueObjects;

public class TIN : ValueObject
{
    public string Value { get; }

    // Parameterless constructor for Entity Framework
    private TIN()
    {
        Value = string.Empty;
    }

    private TIN(string value)
    {
        Value = value;
    }

    public static TIN Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TIN cannot be null or empty", nameof(value));

        // Seun 2026-01-26: Kehinde said to comment this out for now because NRS want to be able to process invoices out of Nigeria
        //if (!IsValidNigerianTIN(value))
        //    throw new ArgumentException("Invalid Nigerian TIN format", nameof(value));

        return new TIN(value);
    }

    private static bool IsValidNigerianTIN(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
            return false;

        if (!tin.Contains('-'))
            return false;

        var tinValue = tin.Split('-');
        if (tinValue.Length < 2 || tinValue.Length > 2)
            return false;

        var cleanTin = tin.Trim().Replace("-", "").Replace(" ", "");

        // Nigerian TIN is 12 digits
        return cleanTin.Length == 12 && cleanTin.All(char.IsDigit);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(TIN tin) => tin.Value;
}