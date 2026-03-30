using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.ProposeBusinessItemPriceChange;

public record ProposeBusinessItemPriceChangeCommand(
    Guid BusinessItemId,
    decimal NewPrice,
    string? Comments = null) : IRequest<ProposeBusinessItemPriceChangeResult>;

public record ProposeBusinessItemPriceChangeResult(
    bool IsSuccess,
    string Message,
    Guid? PriceHistoryId = null);
