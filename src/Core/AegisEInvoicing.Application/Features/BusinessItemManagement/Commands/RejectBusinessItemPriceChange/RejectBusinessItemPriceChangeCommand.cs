using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.RejectBusinessItemPriceChange;

public record RejectBusinessItemPriceChangeCommand(
    Guid PriceHistoryId,
    string? Comments = null) : IRequest<RejectBusinessItemPriceChangeResult>;

public record RejectBusinessItemPriceChangeResult(
    bool IsSuccess,
    string Message);
