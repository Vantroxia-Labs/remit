using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.UserManagement.DTOs;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Queries.GetAegisUsers;

/// <summary>
/// Query to retrieve Aegis users with pagination and filtering
/// Security: Only accessible to Aegis Platform Administrators
/// </summary>
public record GetAegisUsersQuery(
    string? SearchTerm = null,
    UserStatus? Status = null,
    AegisRole? AegisRole = null,
    string? AegisDepartment = null,
    bool? MustChangePassword = null,
    bool? IsEmailVerified = null,
    DateTimeOffset? CreatedAfter = null,
    DateTimeOffset? CreatedBefore = null,
    DateTimeOffset? LastLoginAfter = null,
    DateTimeOffset? LastLoginBefore = null,
    string? SortBy = "CreatedAt", // FirstName, LastName, Email, CreatedAt, LastLoginAt, AegisRole
    bool SortDescending = false,
    int PageNumber = 1,
    int PageSize = 20) : IRequest<PaginatedList<AegisUserSummaryDto>>;
