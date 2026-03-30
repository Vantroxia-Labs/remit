using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetAllLicenses;

/// <summary>
/// Handler for getting all OnPremise business licenses
/// Aegis Admin only - shows all businesses with their license status
/// Optimized: Uses database-level filtering, sorting, and pagination
/// </summary>
public class GetAllLicensesQueryHandler(
    IApplicationDbContext context,
    ILogger<GetAllLicensesQueryHandler> logger) : IRequestHandler<GetAllLicensesQuery, GetAllLicensesResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILogger<GetAllLicensesQueryHandler> _logger = logger;

    public async Task<GetAllLicensesResult> Handle(
        GetAllLicensesQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Fetching all licenses - Page: {PageNumber}, Size: {PageSize}, Status: {Status}, Search: {SearchTerm}",
                request.PageNumber, request.PageSize, request.Status ?? "All", request.SearchTerm ?? "None");

            var now = DateTime.UtcNow;
            var expiringSoonThreshold = now.AddDays(30);

            // Query OnPremise businesses only
            var query = _context.Businesses
                .Where(b => b.DeploymentMode == DeploymentMode.OnPremise)
                .AsNoTracking();

            // Apply search filter at database level
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchLower = request.SearchTerm.ToLower();
                query = query.Where(b =>
                    b.Name.ToLower().Contains(searchLower) ||
                    (b.LicenseKey != null && b.LicenseKey.ToLower().Contains(searchLower)));
            }

            // Apply status filter at database level
            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = request.Status.ToLower() switch
                {
                    "notactivated" => query.Where(b => b.LicenseKeyExpiryDate == null),
                    "expired" => query.Where(b => b.LicenseKeyExpiryDate != null && b.LicenseKeyExpiryDate < now),
                    "expiringsoon" => query.Where(b => b.LicenseKeyExpiryDate != null && 
                                                      b.LicenseKeyExpiryDate >= now && 
                                                      b.LicenseKeyExpiryDate <= expiringSoonThreshold),
                    "active" => query.Where(b => b.LicenseKeyExpiryDate != null && b.LicenseKeyExpiryDate > expiringSoonThreshold),
                    _ => query
                };
            }

            // Get total count BEFORE pagination (database count, not in-memory)
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting at database level (closest to expiry first, then by name)
            var orderedQuery = query.OrderBy(b => b.LicenseKeyExpiryDate ?? DateTime.MaxValue)
                                    .ThenBy(b => b.Name);

            // Apply pagination at database level - ONLY load the needed records
            var businesses = await orderedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(b => new
                {
                    b.Id,
                    b.Name,
                    b.LicenseKey,
                    b.LicenseKeyIssuedDate,
                    b.LicenseKeyExpiryDate
                })
                .ToListAsync(cancellationToken);

            // Map to LicenseInfo with calculated status (only for the paginated results)
            var items = businesses.Select(b =>
            {
                var status = GetLicenseStatus(b.LicenseKeyExpiryDate, now, expiringSoonThreshold);
                var daysRemaining = b.LicenseKeyExpiryDate.HasValue
                    ? (int)(b.LicenseKeyExpiryDate.Value - now).TotalDays
                    : (int?)null;

                return new LicenseInfo
                {
                    BusinessId = b.Id,
                    BusinessName = b.Name,
                    LicenseKey = b.LicenseKey ?? string.Empty,
                    IssuedDateValue = b.LicenseKeyIssuedDate ?? DateTime.MinValue,
                    ExpiryDateValue = b.LicenseKeyExpiryDate ?? DateTime.MinValue,
                    DaysRemaining = daysRemaining ?? 0,
                    Status = status
                };
            }).ToList();

            _logger.LogInformation(
                "Retrieved {Count} licenses from page {PageNumber} (Total: {TotalCount})",
                items.Count, request.PageNumber, totalCount);

            return new GetAllLicensesResult
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all licenses");
            throw;
        }
    }

    private static string GetLicenseStatus(DateTime? expiryDate, DateTime now, DateTime expiringSoonThreshold)
    {
        if (!expiryDate.HasValue)
            return "NotActivated";

        if (expiryDate.Value < now)
            return "Expired";

        if (expiryDate.Value <= expiringSoonThreshold)
            return "ExpiringSoon";

        return "Active";
    }
}

