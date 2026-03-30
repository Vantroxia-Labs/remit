using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Entities.UserManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetBusinessUsers;

public class GetBusinessUserByIdQueryHandler : IRequestHandler<GetBusinessUserByIdQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessUserByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<UserDto?> Handle(GetBusinessUserByIdQuery request, CancellationToken cancellationToken)
    {
        // Validate ID formats to prevent injection attacks (VAPT finding: time-based SQL injection)
        if (!Guid.TryParse(request.businessId, out var businessId) ||
            !Guid.TryParse(request.userId, out var userId))
        {
            return null; // Invalid ID format - return null instead of throwing exception
        }

        var user = await _context.Users.Where(u => u.BusinessId == businessId && u.Id == userId)
            .Include(u => u.RoleAssignments)
                .ThenInclude(ura => ura.PlatformRole)
            .Include(u => u.Business)
            .Include(u => u.Branch)
            .FirstOrDefaultAsync();

        if (user is null)
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
