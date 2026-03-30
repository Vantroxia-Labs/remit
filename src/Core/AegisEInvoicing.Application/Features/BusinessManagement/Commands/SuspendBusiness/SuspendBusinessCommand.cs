using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.SuspendBusiness;

public record SuspendBusinessCommand : IRequest<SuspendBusinessResult>, ITransactionalCommand
{
    public Guid BusinessId { get; init; }
    public string Reason { get; init; } = default!;
}