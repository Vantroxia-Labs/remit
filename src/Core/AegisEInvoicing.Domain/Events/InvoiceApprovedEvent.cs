using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events;

public record InvoiceApprovedEvent(
    Guid InvoiceId,
    string InvoiceReferenceNumber,
    Guid TenantId,
    Guid ApprovedBy) : DomainEvent;