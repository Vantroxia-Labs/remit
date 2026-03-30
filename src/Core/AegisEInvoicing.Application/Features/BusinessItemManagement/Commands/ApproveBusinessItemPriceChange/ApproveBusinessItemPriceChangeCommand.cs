using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.ApproveBusinessItemPriceChange;

public record ApproveBusinessItemPriceChangeCommand(
    Guid PriceHistoryId,
    string? Comments = null) : IRequest<ApproveBusinessItemPriceChangeResult>, ITransactionalCommand;

public record ApproveBusinessItemPriceChangeResult(
    bool IsSuccess,
    string Message,
    decimal? NewPrice = null);
