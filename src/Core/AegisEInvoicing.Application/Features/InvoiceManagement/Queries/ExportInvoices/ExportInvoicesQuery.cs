using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.ExportInvoices;

public record ExportInvoicesQuery : IRequest<ExportInvoicesResult>
{
    public InvoiceStatus? InvoiceStatus { get; init; }
    public DateOnly? StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public string? SearchTerm { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public string? PaymentReference { get; init; }
}
