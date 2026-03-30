using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Handlers;

public class GetBusinessUsersQueryHandler : IRequestHandler<GetBusinessUsersQuery, IEnumerable<BusinessUserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetBusinessUsersQueryHandler> _logger;

    public GetBusinessUsersQueryHandler(
        IApplicationDbContext context,
        ILogger<GetBusinessUsersQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<BusinessUserDto>> Handle(GetBusinessUsersQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var users = await _context.Users
                .AsNoTracking()
                .Where(u => u.BusinessId == request.BusinessId && u.Status == UserStatus.Active)
                .ToListAsync(cancellationToken);

            return users.Select(u => new BusinessUserDto
            {
                Id = u.Id,
                FullName = $"{u.FirstName} {u.LastName}",
                Email = u.Email,
                IsActive = u.Status == UserStatus.Active,
                LastLoginAt = u.LastLoginAt,
                CreatedAt = u.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving business users: {BusinessId}", request.BusinessId);
            throw;
        }
    }
}