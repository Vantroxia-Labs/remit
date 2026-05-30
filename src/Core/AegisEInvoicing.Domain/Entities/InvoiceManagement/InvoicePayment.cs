using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

public class InvoicePayment : AuditableEntity
{
    public Guid? InvoiceId { get; private set; }
    public Guid? ReceivedInvoiceId { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
    public DateTimeOffset PaidAt { get; private set; }

    // Navigation properties
    public Invoice? Invoice { get; private set; }
    public ReceivedInvoice? ReceivedInvoice { get; private set; }

    public static InvoicePayment ForInvoice(Guid invoiceId, decimal amount, string? reference, Guid createdBy)
        => new()
        {
            InvoiceId = invoiceId,
            Amount = amount,
            Reference = reference,
            PaidAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };

    public static InvoicePayment ForReceivedInvoice(Guid receivedInvoiceId, decimal amount, string? reference, Guid createdBy)
        => new()
        {
            ReceivedInvoiceId = receivedInvoiceId,
            Amount = amount,
            Reference = reference,
            PaidAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
}
