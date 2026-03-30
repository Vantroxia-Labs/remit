using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceWithParty;

public record CreateInvoiceWithPartyCommand : IRequest<CreateInvoiceWithPartyResult>, ITransactionalCommand
{
    public Guid BusinessId { get; init; } = Guid.Empty;
    public DateOnly IssueDate { get; init; }
    public TimeOnly? IssueTime { get; init; }
    public InvoiceType InvoiceType { get; init; } = null!;
    public Currency Currency { get; init; } = null!;
    public DeliveryPeriod DeliveryPeriod { get; init; } = null!;
    public PaymentMeans PaymentMeans { get; init; } = null!;
    public DateOnly? DueDate { get; init; }
    public string? Note { get; init; }
    public string? PaymentReference { get; init; }
    public string? PaymentTerms { get; init; }
    
    public CreatePartyDto Party { get; init; } = null!;
    public List<CreateInvoiceItemDto> InvoiceItems { get; init; } = [];
}