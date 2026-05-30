namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;

public record WhtScheduleItemDto
{
    public Guid Id { get; init; }
    public Guid ReceivedInvoiceId { get; init; }
    public string VendorName { get; init; } = null!;
    public string? VendorAddress { get; init; }
    public string? VendorTin { get; init; }
    public string Irn { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
    public string NatureOfTransaction { get; init; } = null!;
    public decimal GrossAmount { get; init; }
    public decimal WhtRate { get; init; }
    public decimal WhtAmount { get; init; }
    public decimal NetAmount { get; init; }
    public string TaxAuthority { get; init; } = null!;
}

public record WhtScheduleDto
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
    public int TotalItemCount { get; init; }
    public decimal TotalGrossAmount { get; init; }
    public decimal TotalWhtAmount { get; init; }
    public decimal TotalNrsWhtAmount { get; init; }
    public decimal TotalStateWhtAmount { get; init; }
    public List<WhtScheduleItemDto> Items { get; init; } = [];
}
