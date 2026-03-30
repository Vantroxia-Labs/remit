using AegisEInvoicing.Domain.ValueObjects;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoice;

public record UpdateInvoiceCommand : IRequest<UpdateInvoiceResult>
{
    public Guid InvoiceId { get; init; }
    public Guid PartyId { get; init; } = Guid.Empty!;
    public DateOnly IssueDate { get; init; }
    public InvoiceType InvoiceType { get; init; } = null!;
    public Currency Currency { get; init; } = null!;
    public DeliveryPeriod DeliveryPeriod { get; init; } = null!;
    public PaymentMeans PaymentMeans { get; init; } = null!;
    public DateOnly? DueDate { get; init; }
    public string? Note { get; init; }
    public string? PaymentReference { get; init; }
    public string? PaymentTerms { get; init; }
}