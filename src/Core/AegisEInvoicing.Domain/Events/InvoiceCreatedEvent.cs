using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events;

public record InvoiceCreatedEvent(
    Guid InvoiceId,
    string InvoiceReferenceNumber,
    Guid TenantId) : DomainEvent;