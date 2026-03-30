using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UpdateInvoiceItem;

public record UpdateInvoiceItemCommand : IRequest<UpdateInvoiceItemResult>, ITransactionalCommand
{
    public Guid? BusinessItemId { get; init; }
    public Guid InvoiceItemId { get; init; }
    public int? Quantity { get; init; }
    public DiscountFeeDto? DiscountFee { get; init; }
    public AdditionalFeeDto? AdditionalFee { get; init; }
}