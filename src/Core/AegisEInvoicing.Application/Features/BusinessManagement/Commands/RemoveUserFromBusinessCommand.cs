using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands;

public record RemoveUserFromBusinessCommand : IRequest<RemoveUserFromBusinessResult>
{
    public Guid BusinessId { get; init; }
    public Guid UserId { get; init; }
}

public record RemoveUserFromBusinessResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
}