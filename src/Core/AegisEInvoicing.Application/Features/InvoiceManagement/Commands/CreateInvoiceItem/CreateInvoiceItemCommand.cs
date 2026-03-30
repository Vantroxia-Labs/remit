using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoiceItem;

public record CreateInvoiceItemCommand : IRequest<CreateInvoiceItemResult>, ITransactionalCommand
{
    public Guid InvoiceId { get; init; }
    public Guid BusinessItemId { get; init; }
    public int Quantity { get; init; }
    public DiscountFeeDto? DiscountFee { get; init; }
    public AdditionalFeeDto? AdditionalFee { get; init; }
}