namespace AegisEInvoicing.Domain.ValueObjects;

public class PaymentMeans : ValueObject
{
    public string Code { get; }
    public string Name { get; set; }

    private PaymentMeans()
    {
        Code = string.Empty;
        Name = string.Empty;
    }

    private PaymentMeans(
        string code,
        string name)
    {
        Code = code;
        Name = name;
    }

    public static PaymentMeans Create(
        string code,
        string name)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Payment means code cannot be null or empty", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Payment means name cannot be null or empty", nameof(name));

        return new PaymentMeans(code, name);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Code;
        yield return Name;
    }
}