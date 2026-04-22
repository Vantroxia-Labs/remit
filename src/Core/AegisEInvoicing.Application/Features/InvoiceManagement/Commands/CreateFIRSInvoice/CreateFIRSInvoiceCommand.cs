using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;

public record CreateFIRSInvoiceCommand : IRequest<CreateFIRSInvoiceResult>, ITransactionalCommand
{
    public Guid BusinessId { get; init; } = Guid.Empty;
    public string? InvoiceNumber { get; init; }
    public DateOnly IssueDate { get; init; }
    public TimeOnly? IssueTime { get; init; }
    public InvoiceType InvoiceType { get; init; } = null!;
    public Currency Currency { get; init; } = null!;
    public DeliveryPeriod DeliveryPeriod { get; init; } = null!;
    public PaymentMeans PaymentMeans { get; init; } = null!;
    public DateOnly? DueDate { get; init; }
    public string? Note { get; init; }
    public string? PaymentReference { get; init; }
    public string? PaymentTerms { get; init; }
    public InvoiceSource InvoiceSource { get; init; } = InvoiceSource.SFTP;
    public InvoiceKind? InvoiceKind { get; init; }

    public CreatePartyDto Party { get; init; } = null!;
    public List<InvoiceItemRequest> InvoiceItems { get; init; } = [];
    public List<BillingReferenceRequest>? BillingReferences { get; init; }

    // Document References (all optional)
    public DocumentReferenceRequest? DispatchDocumentReference { get; init; }
    public DocumentReferenceRequest? ReceiptDocumentReference { get; init; }
    public DocumentReferenceRequest? OriginatorDocumentReference { get; init; }
    public DocumentReferenceRequest? ContractDocumentReference { get; init; }
    public List<DocumentReferenceRequest>? AdditionalDocumentReferences { get; init; }
}

// DTOs for the invoice items with full item details
public record InvoiceItemRequest
{
    public string Name { get; init; } = null!;
    public string ItemDescription { get; init; } = null!;
    public ServiceCodeRequest ServiceCode { get; init; } = null!;;
    public List<TaxCategoryRequest> TaxCategories { get; init; } = [];
    public decimal UnitPrice { get; init; }
    public decimal Quantity { get; init; }
    public DiscountFeeDto? DiscountFee { get; init; }
    public AdditionalFeeDto? AdditionalFee { get; init; }
}

public record ServiceCodeRequest
{
    public string Code { get; init; } = null!;
    public string Name { get; init; } = null!;
}

public record TaxCategoryRequest
{
    public string Name { get; init; } = null!;
    public bool IsPercentage { get; init; }
    public decimal? Percent { get; init; }
    public decimal? FlatAmount { get; init; }
}

public record BillingReferenceRequest
{
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
}

public record DocumentReferenceRequest
{
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
}
