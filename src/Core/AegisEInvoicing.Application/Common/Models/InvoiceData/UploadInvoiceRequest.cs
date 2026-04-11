using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Application.Common.Models.InvoiceData;

public class UploadInvoiceRequest
{
    public string IssueDate { get; init; } = null!;
    public string DueDate { get; init; } = null!;
    public string IssueTime { get; init; } = null!;
    public InvoiceTypeRequest InvoiceType { get; set; } = null!;
    public string? InvoiceKind { get; set; }
    public CurrencyRequest Currency { get; set; } = null!;
    public DeliveryPeriodRequest DeliveryPeriod { get; set; } = null!;
    public PaymentMeansRequest PaymentMeans { get; set; } = null!;
    public string Note { get; set; } = null!;
    public string PaymentReference { get; set; } = null!;
    public string PaymentTerms { get; set; } = null!;
    public PartyRequest Party { get; set; } = null!;
    public List<InvoiceItemRequest> InvoiceItems { get; set; } = [];
    public List<CreateBillingReferenceDto>? BillingReference { get; init; }
    public CreateDocumentReferenceDto? DispatchDocumentReference { get; init; }
    public CreateDocumentReferenceDto? ReceiptDocumentReference { get; init; }
    public CreateDocumentReferenceDto? OriginatorDocumentReference { get; init; }
    public CreateDocumentReferenceDto? ContractDocumentReference { get; init; }
    public List<CreateDocumentReferenceDto>? AdditionalDocumentReferences { get; init; }
}

public class InvoiceTypeRequest
{
    public string Name { get; set; } = null!;
    public int Code { get; set; }
}

public class CurrencyRequest
{
    public string Name { get; set; } = null!;
    [StringLength(3)]
    public string Code { get; set; } = null!;
}

public class DeliveryPeriodRequest
{
    public string StartDate { get; set; } = null!;
    public string EndDate { get; set; } = null!;
}

public class PaymentMeansRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class PartyRequest
{
    public string Name { get; set; } = null!;

    [StringLength(500, MinimumLength = 10)]
    public string Description { get; set; } = null!;
    public string Phone { get; set; } = null!;
    [EmailAddress]
    public string Email { get; set; } = null!;
    public string TaxIdentificationNumber { get; set; } = null!;
    public AddressRequest Address { get; set; } = null!;
}

public class AddressRequest
{
    public string Street { get; set; } = null!;
    public string City { get; set; } = null!;
    public string State { get; set; } = null!;
    [StringLength(2)]
    public string Country { get; set; } = null!;
    public string PostalCode { get; set; } = null!;
}

public class InvoiceItemRequest
{
    public string Name { get; set; } = null!;
    [StringLength(500, MinimumLength = 10)]
    public string ItemDescription { get; set; } = null!;
    public string ItemCategory { get; set; } = null!;
    public ServiceCodeRequest ServiceCode { get; set; } = null!;
    public List<TaxCategoryRequest> TaxCategories { get; set; } = [];
    public decimal UnitPrice { get; set; }
    public decimal Quantity { get; set; }
    public FeeRequest DiscountFee { get; set; } = null!;
    public FeeRequest AdditionalFee { get; set; } = null!;
}

public class ServiceCodeRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public class TaxCategoryRequest
{
    public string Name { get; set; } = null!;
    public bool IsPercentage { get; set; }
    public decimal? Percent { get; set; }
    public decimal? FlatAmount { get; set; }
}

public class FeeRequest
{
    public decimal Amount { get; set; }
    public string FeeStandardUnit { get; set; } = null!;
}