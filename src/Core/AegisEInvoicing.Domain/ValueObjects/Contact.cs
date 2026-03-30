namespace AegisEInvoicing.Domain.ValueObjects;

public class Contact : ValueObject
{
    public string? Telephone { get; }
    public string? ElectronicMail { get; }

    private Contact()
    {
    }

    private Contact(string? telephone, string? electronicMail)
    {
        Telephone = telephone;
        ElectronicMail = electronicMail;
    }

    public static Contact Create(string? telephone = null, string? electronicMail = null)
    {
        return new Contact(telephone?.Trim(), electronicMail?.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Telephone ?? string.Empty;
        yield return ElectronicMail ?? string.Empty;
    }
}