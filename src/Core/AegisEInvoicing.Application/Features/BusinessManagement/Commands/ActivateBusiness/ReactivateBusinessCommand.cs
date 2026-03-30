using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateBusiness;

public record ReactivateBusinessCommand : IRequest<ReactivateBusinessResult>, ITransactionalCommand
{
    public Guid BusinessId { get; init; }
    public string Reason { get; init; } = default!;
}