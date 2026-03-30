using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events;

public record InvoiceSignedEvent(
    Guid InvoiceId,
    string InvoiceReferenceNumber,
    Guid TenantId) : DomainEvent;