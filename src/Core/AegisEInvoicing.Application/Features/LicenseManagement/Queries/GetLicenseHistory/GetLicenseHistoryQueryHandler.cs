using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetLicenseHistory;

/// <summary>
/// Handler for getting current business's license information
/// Client Admin only
/// </summary>
public class GetLicenseHistoryQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<GetLicenseHistoryQueryHandler> logger) : IRequestHandler<GetLicenseHistoryQuery, GetLicenseHistoryResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<GetLicenseHistoryQueryHandler> _logger = logger;

    public async Task<GetLicenseHistoryResult> Handle(
        GetLicenseHistoryQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Ensure user is authenticated
            if (!_currentUserService.UserId.HasValue)
            {
                _logger.LogWarning("Unauthenticated user attempted to get license history");
                throw new UnauthorizedAccessException("User is not authenticated");
            }

            if (!_currentUserService.BusinessId.HasValue)
            {
                _logger.LogWarning(
                    "User {UserId} without business attempted to get license history",
                    _currentUserService.UserId);
                throw new InvalidOperationException("User does not belong to a business");
            }

            _logger.LogInformation(
                "Fetching license history for business {BusinessId}",
                _currentUserService.BusinessId);

            // Base query with search filter (applied at database level)
            var query = _context.Businesses
                .AsNoTracking()
                .Where(b => b.Id == _currentUserService.BusinessId.Value);

            // Apply search term filter if provided
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                query = query.Where(b 
                    => b.LicenseKey != null 
                    && b.LicenseKey.Contains(request.SearchTerm));
            
            // Get business
            var business = await query.Select(s => new LicenseDetails
                {
                    LicenseKey = s.LicenseKey,
                    IssuedDate = s.LicenseKeyIssuedDate,
                    ExpiryDate = s.LicenseKeyExpiryDate,
                    DaysRemaining = s.LicenseKeyExpiryDate.HasValue
                        ? (int)(s.LicenseKeyExpiryDate.Value - DateTime.UtcNow).TotalDays
                        : (int?)null
                })
                .ToListAsync(cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Retrieved license history for business {BusinessId}",
                _currentUserService.BusinessId.Value);

            // Get total count after filtering
            var totalCount = business.Count;
            var sortedData = business
                .OrderBy(x => x.ExpiryDate ?? DateTime.MaxValue)
                .ToList();

            // Apply pagination
            var pagedData = sortedData
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            return new GetLicenseHistoryResult
            {
                Items = business,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching license history for business {BusinessId}",
                _currentUserService.BusinessId);
            throw;
        }
    }
}
