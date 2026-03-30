using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Models;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;

public record InvoiceStatusDto
{
    public Guid InvoiceId { get; init; }
    public Guid BusinessId { get; init; }
    public string BusinessName { get; init; } = null!;
    public string IRN { get; init; } = null!;
    public DateOnly IssueDate { get; init; }
    public InvoiceStatus InvoiceStatus { get; init; }
    public PaymentStatus PaymentStatus { get; init; }
    public string? FIRSSubmissionId { get; init; }
    public DateTimeOffset? SubmittedToFIRSAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
    public string? Note { get; init; }
    public string CreatedBy { get; init; } = null!;
}

public record GetInvoiceStatusResult : GenericResult
{
    public InvoiceStatusDto? InvoiceStatus { get; set; }
}