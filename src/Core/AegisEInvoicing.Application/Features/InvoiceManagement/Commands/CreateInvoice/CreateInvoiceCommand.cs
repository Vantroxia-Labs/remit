using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;

public record CreateInvoiceCommand : IRequest<CreateInvoiceResult>, ITransactionalCommand
{
    public Guid PartyId { get; init; } = Guid.Empty!;
    public DateOnly IssueDate { get; init; }
    public InvoiceType InvoiceType { get; init; } = null!;
    public Domain.Enums.InvoiceKind? InvoiceKind { get; init; }
    public Currency Currency { get; init; } = null!;
    public DeliveryPeriod DeliveryPeriod { get; init; } = null!;
    public PaymentMeans PaymentMeans { get; init; } = null!;
    public DateOnly? DueDate { get; init; }
    public string? Note { get; init; }
    public string? PaymentReference { get; init; }
    public string? PaymentTerms { get; init; }

    public List<CreateInvoiceItemDto> InvoiceItems { get; init; } = [];
    public List<CreateBillingReferenceDto>? BillingReferences { get; init; }

    // Document References (all optional)
    public CreateDocumentReferenceDto? DispatchDocumentReference { get; init; }
    public CreateDocumentReferenceDto? ReceiptDocumentReference { get; init; }
    public CreateDocumentReferenceDto? OriginatorDocumentReference { get; init; }
    public CreateDocumentReferenceDto? ContractDocumentReference { get; init; }
    public List<CreateDocumentReferenceDto>? AdditionalDocumentReferences { get; init; }
}

public record CreateBillingReferenceDto
{
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
}

public record CreateDocumentReferenceDto
{
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
}