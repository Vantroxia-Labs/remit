namespace AegisEInvoicing.Domain.ValueObjects;

public class OrderReference : ValueObject
{
    public string? Id { get; }
    public string? SalesOrderId { get; }
    public string? Uuid { get; }

    private OrderReference()
    {
    }

    private OrderReference(string? id, string? salesOrderId, string? uuid)
    {
        Id = id;
        SalesOrderId = salesOrderId;
        Uuid = uuid;
    }

    public static OrderReference Create(string? id = null, string? salesOrderId = null, string? uuid = null)
    {
        return new OrderReference(
            id?.Trim(),
            salesOrderId?.Trim(),
            uuid?.Trim());
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id ?? string.Empty;
        yield return SalesOrderId ?? string.Empty;
        yield return Uuid ?? string.Empty;
    }
}