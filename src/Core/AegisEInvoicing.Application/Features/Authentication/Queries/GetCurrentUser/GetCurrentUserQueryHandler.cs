using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.Authentication.Queries.GetCurrentUser;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetCurrentUserQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto?> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
            return null;

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.RoleAssignments)
                .ThenInclude(ura => ura.PlatformRole)
            .Include(u => u.Business)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync(u => u.Id == _currentUserService.UserId.Value, cancellationToken);

        if (user == null)
            return null;

        return new UserDto(
            user.Id,
            user.BusinessId,
            user.BranchId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.PhoneNumber,
            user.Status.ToString(),
            user.IsEmailVerified,
            user.LastLoginAt,
            user.MustChangePassword,
            user.FailedLoginAttempts,
            user.LockedOutUntil,
            user.RoleAssignments
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
                user.Preferences.Language,
                user.Preferences.TimeZone,
                user.Preferences.DateFormat,
                user.Preferences.NumberFormat,
                user.Preferences.EmailNotifications,
                user.Preferences.SmsNotifications,
                user.Preferences.InvoiceReminders,
                user.Preferences.SecurityAlerts,
                user.Preferences.Theme,
                user.Preferences.PageSize,
                user.Preferences.TwoFactorEnabled),
            user.Business?.Name,
            user.Branch?.Name,
            user.BranchId == null,
            user.BranchId != null,
            user.CreatedAt,
            user.UpdatedAt);
    }
}