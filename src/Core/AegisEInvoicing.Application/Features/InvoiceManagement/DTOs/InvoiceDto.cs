using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = null!;
    public string Irn { get; init; } = null!;
    public InvoiceSource InvoiceSource { get; init; }
    public InvoiceStatus CurrentInvoiceStatus { get; init; }
    public string? FirsResponseMessage { get; init; } = string.Empty;
    public InvoiceStatus[] InvoiceStatus { get; init; } = null!;
    public PaymentStatus PaymentStatus { get; init; }
    public DateOnly IssueDate { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CreatedBy { get; init; } = null!;
}
