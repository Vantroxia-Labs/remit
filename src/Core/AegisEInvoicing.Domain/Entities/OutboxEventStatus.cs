namespace AegisEInvoicing.Domain.Entities;

public enum OutboxEventStatus
{
    Pending = 0,
    Processing = 1,
    Processed = 2,
    Failed = 3
}