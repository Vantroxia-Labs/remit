using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.Events;

public record InvoiceSubmittedEvent(
    Guid InvoiceId,
    string InvoiceReferenceNumber,
    Guid TenantId,
    string FIRSSubmissionId) : DomainEvent;