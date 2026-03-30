using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands;

public record AddUserToBusinessCommand : IRequest<AddUserToBusinessResult>
{
    public Guid BusinessId { get; init; }
    public Guid UserId { get; init; }
    public string Role { get; init; } = default!;
}

public record AddUserToBusinessResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
}