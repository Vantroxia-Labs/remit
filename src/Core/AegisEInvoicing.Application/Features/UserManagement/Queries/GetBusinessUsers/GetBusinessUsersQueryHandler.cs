using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetBusinessUsers;

public class GetBusinessUsersQueryHandler : IRequestHandler<GetBusinessUsersQuery, PaginatedList<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessUsersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;

    }

    public async Task<PaginatedList<UserDto>> Handle(GetBusinessUsersQuery request, CancellationToken cancellationToken)
    {
        // Validate ID format to prevent injection attacks (VAPT finding: time-based SQL injection)
        if (!Guid.TryParse(request.businessId, out var businessId))
        {
            return new PaginatedList<UserDto>([], 0, request.pageNumber, request.pageSize);
        }

        var query = _context.Users.Where(u => u.BusinessId == businessId)
            .Include(u => u.RoleAssignments)
                .ThenInclude(ura => ura.PlatformRole)
            .Include(u => u.Business)
            .Include(u => u.Branch)
            .AsQueryable();        

        // Apply security filters based on current user context
        if (_currentUserService.IsBusinessLevel)
        {
            // Merchant admins can see all users in their merchant and branches
            query = query.Where(u => u.BusinessId == _currentUserService.BusinessId);
        }
        else if (_currentUserService.IsBranchLevel)
        {
            // Branch admins can only see users in their branch
            query = query.Where(u => u.BranchId == _currentUserService.BranchId);
        }
        else if (!_currentUserService.IsPlatformAdmin)
        {
            // Regular users can only see themselves
            query = query.Where(u => u.Id == _currentUserService.UserId);
        }        

        var totalCount = await query.CountAsync(cancellationToken);

        //var user = query

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Select(u => new UserDto(
                u.Id,
                u.BusinessId,
                u.BranchId,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.Status.ToString(),
                u.IsEmailVerified,
                u.LastLoginAt,
                u.MustChangePassword,
                u.FailedLoginAttempts,
                u.LockedOutUntil,
                u.RoleAssignments
                    .Where(ura => ura.IsActive && !(ura.ExpiresAt <= DateTimeOffset.UtcNow))
                    .Select(ura => new UserRoleDto(
                        ura.Id,
                        ura.PlatformRoleId,
                        ura.PlatformRole.Name,
                        ura.PlatformRole.Description,
                        ura.PlatformRole.Category,
                        ura.AssignedAt,
                        ura.ExpiresAt,
                        ura.RevokedAt,
                        ura.RevocationReason,
                        ura.IsActive)),
                new UserPreferencesDto(
                    u.Preferences.Language,
                    u.Preferences.TimeZone,
                    u.Preferences.DateFormat,
                    u.Preferences.NumberFormat,
                    u.Preferences.EmailNotifications,
                    u.Preferences.SmsNotifications,
                    u.Preferences.InvoiceReminders,
                    u.Preferences.SecurityAlerts,
                    u.Preferences.Theme,
                    u.Preferences.PageSize,
                    u.Preferences.TwoFactorEnabled),
                u.Business != null ? u.Business.Name : null,
                u.Branch != null ? u.Branch.Name : null,
                u.BranchId == null,
                u.BranchId != null,
                u.CreatedAt,
                u.UpdatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<UserDto>(users, totalCount, request.pageNumber, request.pageSize);
    }

}
