using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;

public record VatScheduleItemDto
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public string InvoiceCode { get; init; } = null!;
    public string? Irn { get; init; }
    public string PartyName { get; init; } = null!;
    public string? PartyTin { get; init; }
    public DateOnly IssueDate { get; init; }
    public decimal TaxableAmount { get; init; }
    public decimal VatAmount { get; init; }
    public decimal TotalAmount { get; init; }
    public string PaymentStatus { get; init; } = null!;
}

public record VatScheduleDto
{
    public Guid Id { get; init; }
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = null!;
    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }
    public DateOnly DueDate { get; init; }
    public string Status { get; init; } = null!;
    public DateTimeOffset? FiledAt { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public int TotalInvoiceCount { get; init; }
    public decimal TotalTaxableAmount { get; init; }
    public decimal TotalVatAmount { get; init; }
    public List<VatScheduleItemDto> Items { get; init; } = [];
}
