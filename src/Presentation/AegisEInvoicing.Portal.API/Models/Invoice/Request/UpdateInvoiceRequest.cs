using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

namespace AegisEInvoicing.Portal.API.Models.Invoice.Request;

public class UpdateInvoiceRequest
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
}

public record UpdateInvoicePaymentStatusRequest(PaymentStatus PaymentStatus);