namespace AegisEInvoicing.Domain.ValueObjects;

public class IRN : ValueObject
{
    public string Value { get; }

    // Parameterless constructor for Entity Framework
    private IRN()
    {
        Value = string.Empty;
    }

    private IRN(string value)
    {
        Value = value;
    }

    public static IRN Create(string prefix, string serviceId, int sequenceNumber, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new ArgumentException("IRN prefix cannot be null or empty", nameof(prefix));

        if (string.IsNullOrWhiteSpace(serviceId))
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));

        if (sequenceNumber <= 0)
            throw new ArgumentException("Sequence number must be greater than zero", nameof(sequenceNumber));

        var formattedSequence = sequenceNumber.ToString("D8"); // 8 digits with leading zeros
        var formattedDate = date.ToString("yyyyMMdd");
        var value = $"{prefix}{formattedSequence}-{serviceId.ToUpperInvariant()}-{formattedDate}";

        return new IRN(value);
    }

    public static IRN Create(string invoiceNumber, string serviceId, DateOnly date)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
            throw new ArgumentException("invoiceNumber cannot be null or empty", nameof(invoiceNumber));

        if (string.IsNullOrWhiteSpace(serviceId))
            throw new ArgumentException("Service ID cannot be null or empty", nameof(serviceId));
              
        var formattedDate = date.ToString("yyyyMMdd");
        var value = $"{invoiceNumber}-{serviceId.ToUpperInvariant()}-{formattedDate}";

        return new IRN(value);
    }

    public static IRN CreateFromString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IRN value cannot be null or empty", nameof(value));

        if (!IsValidIRNFormat(value))
            throw new ArgumentException("Invalid IRN format. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD", nameof(value));

        return new IRN(value.Trim().ToUpperInvariant());
    }

    public static bool IsValidIRNFormat(string irn)
    {
        if (string.IsNullOrWhiteSpace(irn))
            return false;

        var parts = irn.Split('-');
        if (parts.Length != 3)
            return false;

        // Validate first part (invoice number/reference — can be any non-empty alphanumeric)
        var firstPart = parts[0];
        if (string.IsNullOrWhiteSpace(firstPart) || !firstPart.All(char.IsLetterOrDigit))
            return false;

        // Validate second part (serviceId - alphanumeric, 8 characters)
        if (parts[1].Length != 8 || !parts[1].All(char.IsLetterOrDigit))
            return false;

        // Validate date (should be 8 digits in YYYYMMDD format)
        if (parts[2].Length != 8 || !parts[2].All(char.IsDigit))
            return false;

        return DateTimeOffset.TryParseExact(parts[2], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out _);
    }

    public string GetPrefix()
    {
        var firstPart = Value.Split('-')[0];
        var prefix = new string(firstPart.TakeWhile(char.IsLetter).ToArray());
        return string.IsNullOrEmpty(prefix) ? string.Empty : prefix;
    }

    public int GetSequenceNumber()
    {
        var firstPart = Value.Split('-')[0];
        var prefixLength = firstPart.TakeWhile(char.IsLetter).Count();

        // If no prefix (pure digits), parse the whole first part
        if (prefixLength == 0)
            return int.TryParse(firstPart, out var num) ? num : 0;

        // If has prefix, parse the numeric part after the prefix
        var sequencePart = firstPart.Substring(prefixLength);
        return int.TryParse(sequencePart, out var seqNum) ? seqNum : 0;
    }

    public string GetServiceId()
    {
        return Value.Split('-')[1];
    }

    public DateTimeOffset GetDate()
    {
        var datePart = Value.Split('-')[2];
        return DateTimeOffset.ParseExact(datePart, "yyyyMMdd", null, System.Globalization.DateTimeStyles.AssumeUniversal);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(IRN irn) => irn.Value;
}