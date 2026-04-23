using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceItemDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public string ItemCode { get; init; } = null!;
    public ServiceCode ServiceCode { get; init; } = null!;
    public string Category { get; init; } = null!;
    public string ItemDescription { get; init; } = null!;
    public DiscountFee? DiscountFee { get; init; }
    public AdditionalFee? AdditionalFee { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public decimal Quantity { get; init; }
    public List<BusinessItemTaxCategoryDto> TaxCategories { get; init; } = [];
}
