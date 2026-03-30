using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetUsers;

public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetUsersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users
            .AsNoTracking()
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

        // Apply additional filters
        if (request.BusinessId.HasValue)
        {
            query = query.Where(u => u.BusinessId == request.BusinessId.Value);
        }

        if (request.BranchId.HasValue)
        {
            query = query.Where(u => u.BranchId == request.BranchId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(u => 
                u.FirstName.ToLower().Contains(searchTerm) ||
                u.LastName.ToLower().Contains(searchTerm) ||
                u.Email.ToLower().Contains(searchTerm));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        if (request.IsEmailVerified.HasValue)
        {
            query = query.Where(u => u.IsEmailVerified == request.IsEmailVerified.Value);
        }

        if (request.MustChangePassword.HasValue)
        {
            query = query.Where(u => u.MustChangePassword == request.MustChangePassword.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
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
                    .Where(ura => ura.IsActive && !ura.IsExpired())
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

        return new PaginatedList<UserDto>(users, totalCount, request.PageNumber, request.PageSize);
    }
}