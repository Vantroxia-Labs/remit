using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries;

public record GetBusinessUsersQuery : IRequest<IEnumerable<BusinessUserDto>>
{
    public Guid BusinessId { get; init; }
}

public record BusinessUserDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = default!;
    public string Email { get; init; } = default!;
    public bool IsActive { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}