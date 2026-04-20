using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetAegisUserById;

/// <summary>
/// Handler for retrieving a single Aegis user by ID
/// Security: Only accessible to Aegis Platform Administrators
/// </summary>
public class GetAegisUserByIdQueryHandler : IRequestHandler<GetAegisUserByIdQuery, AegisUserDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAegisUserByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AegisUserDto?> Handle(GetAegisUserByIdQuery request, CancellationToken cancellationToken)
    {
        // Security validation - ensure user is authenticated Aegis Platform Admin
        if (!_currentUserService.IsAuthenticated || !_currentUserService.IsAegisUser)
        {
            return null; // Return null for unauthorized access
        }

        // Additional security check - verify platform admin role
        if (!_currentUserService.HasRole(RoleConstants.AegisAdmin))
        {
            return null; // Return null for unauthorized access
        }

        // Query for Aegis user only
        var AegisUser = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId && u.IsAegisUser)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.Status,
                u.IsEmailVerified,
                u.LastLoginAt,
                u.MustChangePassword,
                u.FailedLoginAttempts,
                u.LockedOutUntil,
                u.AegisRole,
                u.AegisEmployeeId,
                u.AegisDepartment,
                u.LastAegisActivityAt,
                u.CreatedAt,
                u.UpdatedAt,
                u.CreatedBy
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (AegisUser == null)
        {
            return null;
        }

        // Get the creator's name
        string createdByName = "System";
        if (AegisUser.CreatedBy != Guid.Empty)
        {
            var creator = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == AegisUser.CreatedBy)
                .Select(u => $"{u.FirstName} {u.LastName}")
                .FirstOrDefaultAsync(cancellationToken);
            
            if (!string.IsNullOrWhiteSpace(creator))
            {
                createdByName = creator;
            }
        }

        // Load permissions from the user's custom AegisStaff role (if any)
        var customRoleName = $"AegisStaff_{AegisUser.Id:N}";
        var customRole = await _context.PlatformRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Name == customRoleName && !r.IsDeleted, cancellationToken);
        IReadOnlyList<string> permissions = customRole?.Permissions.ToList() ?? [];

        return new AegisUserDto(
            AegisUser.Id,
            AegisUser.FirstName,
            AegisUser.LastName,
            AegisUser.Email,
            AegisUser.PhoneNumber,
            AegisUser.Status.ToString(),
            AegisUser.IsEmailVerified,
            AegisUser.LastLoginAt,
            AegisUser.MustChangePassword,
            AegisUser.FailedLoginAttempts,
            AegisUser.LockedOutUntil,
            AegisUser.AegisRole!.Value, // Safe since we filtered for Aegis users
            AegisUser.AegisEmployeeId,
            AegisUser.AegisDepartment,
            AegisUser.LastAegisActivityAt,
            AegisUser.CreatedAt,
            AegisUser.UpdatedAt,
            createdByName,
            permissions);
    }
}
