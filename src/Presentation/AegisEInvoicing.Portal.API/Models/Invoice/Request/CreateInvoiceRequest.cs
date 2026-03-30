using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

namespace AegisEInvoicing.Portal.API.Models.Invoice.Request;

public class CreateInvoiceRequest
{
    public Guid PartyId { get; init; } = Guid.Empty!;
    public DateOnly IssueDate { get; init; }
    public InvoiceTypeDto InvoiceType { get; init; } = null!;
    public CurrencyDto Currency { get; init; } = null!;
    public DeliveryPeriodDto DeliveryPeriod { get; init; } = null!;
    public PaymentMeansDto PaymentMeans { get; init; } = null!;
    public DateOnly? DueDate { get; init; }    
    public string? Note { get; init; }
    public string? PaymentReference { get; init; }
    public string? PaymentTerms { get; init; }
    public List<CreateInvoiceItemDto> InvoiceItems { get; init; } = [];

    public List<CreateBillingReferenceDto>? BillingReference { get; init; }

    /// <summary>
    /// Optional reference to dispatch/shipping document
    /// </summary>
    public CreateDocumentReferenceDto? DispatchDocumentReference { get; init; }

    /// <summary>
    /// Optional reference to receipt/acknowledgment document
    /// </summary>
    public CreateDocumentReferenceDto? ReceiptDocumentReference { get; init; }

    /// <summary>
    /// Optional reference to originator/source document
    /// </summary>
    public CreateDocumentReferenceDto? OriginatorDocumentReference { get; init; }

    /// <summary>
    /// Optional reference to contract/agreement document
    /// </summary>
    public CreateDocumentReferenceDto? ContractDocumentReference { get; init; }

    /// <summary>
    /// Optional list of additional supporting document references
    /// </summary>
    public List<CreateDocumentReferenceDto>? AdditionalDocumentReferences { get; init; }
}
