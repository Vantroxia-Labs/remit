using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceDetailsDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = null!;
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
    public DateOnly? DueDate { get; init; }
    public TimeOnly? IssueTime { get; init; }
    public InvoiceType InvoiceType { get; init; } = null!;
    public InvoiceSource InvoiceSource { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public string? QrCodeImage { get; init; }
    public string? Note { get; init; }
    public Currency Currency { get; init; } = null!;
    public string? PaymentTerms { get; init; }
    public InvoiceStatus CurrentInvoiceStatus { get; init; }
    public string? FirsResponseMessage { get; init; } = string.Empty;
    public InvoiceStatus[] InvoiceStatus { get; init; } = null!;
    public string? FIRSSubmissionId { get; init; }
    public string? FIRSubmissionResponse { get; init; }
    public DateTimeOffset? SubmittedToFIRSAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public List<InvoiceItemDto> InvoiceItems { get; init; } = [];
    public PartyDto Party { get; init; } = null!;
    public PaymentMeans PaymentMeans { get; init; } = null!;
    public DeliveryPeriod DeliveryPeriod { get; init; } = null!;
    public List<InvoiceApprovalHistoryDto> InvoiceApprovalHistories { get; init; } = [];
    public List<BillingReferenceDto>? BillingReferences { get; init; }

    // Document References (all optional)
    public DocumentReferenceDto? DispatchDocumentReference { get; init; }
    public DocumentReferenceDto? ReceiptDocumentReference { get; init; }
    public DocumentReferenceDto? OriginatorDocumentReference { get; init; }
    public DocumentReferenceDto? ContractDocumentReference { get; init; }
    public List<DocumentReferenceDto>? AdditionalDocumentReferences { get; init; }
}

public record BillingReferenceDto
{
    public Guid Id { get; init; }
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
}

public record InvoiceApprovalHistoryDto
{
    public InvoiceStatus InvoiceStatus { get; init; }
    public Guid PerformedById { get; init; }
    public string PerformedBy { get; init; } = null!;
    public string Comments { get; init; } = string.Empty;
}