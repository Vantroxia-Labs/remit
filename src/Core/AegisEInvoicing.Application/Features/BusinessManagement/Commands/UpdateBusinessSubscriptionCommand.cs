using AegisEInvoicing.Domain.Entities.BusinessManagement;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands;

public record UpdateBusinessSubscriptionCommand : IRequest<BusinessManagementCommandResult>
{
    public Guid BusinessId { get; init; }
    public Guid PlatformSubscriptionId { get; init; }
}

public record BusinessManagementCommandResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
}