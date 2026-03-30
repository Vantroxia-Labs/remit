using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetAegisUsers;

/// <summary>
/// Handler for retrieving Aegis users with pagination and filtering
/// Security: Only accessible to Aegis Platform Administrators
/// </summary>
public class GetAegisUsersQueryHandler : IRequestHandler<GetAegisUsersQuery, PaginatedList<AegisUserSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetAegisUsersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<AegisUserSummaryDto>> Handle(GetAegisUsersQuery request, CancellationToken cancellationToken)
    {
        // Security validation - ensure user is authenticated Aegis Platform Admin
        if (!_currentUserService.IsAuthenticated || !_currentUserService.IsAegisUser)
        {
            return PaginatedList<AegisUserSummaryDto>.Empty(request.PageNumber, request.PageSize);
        }

        // Additional security check - verify platform admin role
        if (!_currentUserService.HasRole(RoleConstants.AegisAdmin))
        {
            return PaginatedList<AegisUserSummaryDto>.Empty(request.PageNumber, request.PageSize);
        }

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsAegisUser) // Only Aegis users
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Sanitize search term to prevent SQL injection (VAPT finding)
            var searchTerm = InputSanitizationService.SanitizeSearchTerm(request.SearchTerm);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchTerm) ||
                    u.LastName.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm) ||
                    (u.AegisEmployeeId != null && u.AegisEmployeeId.ToLower().Contains(searchTerm)) ||
                    (u.AegisDepartment != null && u.AegisDepartment.ToLower().Contains(searchTerm)));
            }
        }

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        if (request.AegisRole.HasValue)
        {
            query = query.Where(u => u.AegisRole == request.AegisRole.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.AegisDepartment))
        {
            // Sanitize department filter to prevent SQL injection (VAPT finding)
            var departmentFilter = InputSanitizationService.SanitizeSearchTerm(request.AegisDepartment);
            if (!string.IsNullOrEmpty(departmentFilter))
            {
                query = query.Where(u => u.AegisDepartment != null &&
                    u.AegisDepartment.ToLower().Contains(departmentFilter));
            }
        }

        if (request.MustChangePassword.HasValue)
        {
            query = query.Where(u => u.MustChangePassword == request.MustChangePassword.Value);
        }

        if (request.IsEmailVerified.HasValue)
        {
            query = query.Where(u => u.IsEmailVerified == request.IsEmailVerified.Value);
        }

        if (request.CreatedAfter.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= request.CreatedAfter.Value);
        }

        if (request.CreatedBefore.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= request.CreatedBefore.Value);
        }

        if (request.LastLoginAfter.HasValue)
        {
            query = query.Where(u => u.LastLoginAt >= request.LastLoginAfter.Value);
        }

        if (request.LastLoginBefore.HasValue)
        {
            query = query.Where(u => u.LastLoginAt <= request.LastLoginBefore.Value);
        }

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        var totalCount = await query.CountAsync(cancellationToken);

        var AegisUsers = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new AegisUserSummaryDto(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.Status.ToString(),
                u.AegisRole!.Value, // Safe since we filtered for Aegis users
                u.AegisRole!.Value.GetDisplayName(),
                u.AegisEmployeeId,
                u.AegisDepartment,
                u.LastLoginAt,
                u.MustChangePassword,
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<AegisUserSummaryDto>(AegisUsers, totalCount, request.PageNumber, request.PageSize);
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, string? sortBy, bool sortDescending)
    {
        Expression<Func<User, object>> keySelector = sortBy?.ToLower() switch
        {
            "firstname" => u => u.FirstName,
            "lastname" => u => u.LastName,
            "email" => u => u.Email,
            "Aegisrole" => u => u.AegisRole!,
            "lastloginat" => u => u.LastLoginAt ?? DateTimeOffset.MinValue,
            "createdat" or _ => u => u.CreatedAt
        };

        return sortDescending 
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }
}
