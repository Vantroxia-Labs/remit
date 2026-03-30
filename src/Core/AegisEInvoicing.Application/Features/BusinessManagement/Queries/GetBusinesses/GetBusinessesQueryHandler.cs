using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinesses;

public class GetBusinessesQueryHandler : IRequestHandler<GetBusinessesQuery, PaginatedList<BusinessUsersSummaryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessesQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<PaginatedList<BusinessUsersSummaryDto>> Handle(GetBusinessesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Businesses
            .AsNoTracking()
            .Include(b => b.AdminUser)
            .Include(b => b.Subscription)
            .AsQueryable();

        // Apply security filters based on current user context
        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
        {
            query = query.Where(b => b.Id == _currentUserService.BusinessId!.Value);
        }

        // Apply additional filters
        if (request.BusinessId.HasValue)
        {
            query = query.Where(b => b.Id == request.BusinessId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Sanitize search term to prevent SQL injection (VAPT finding)
            var searchTerm = InputSanitizationService.SanitizeSearchTerm(request.SearchTerm);
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(b =>
                    b.Name.ToLower().Contains(searchTerm) ||
                    b.TaxIdentificationNumber.Value.ToLower().Contains(searchTerm) ||
                    b.ContactEmail.ToLower().Contains(searchTerm));
            }
        }

        if (request.Status.HasValue)
        {
            query = query.Where(b => b.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var businesses = await query
            .OrderBy(b => b.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(b => new BusinessUsersSummaryDto(
                b.Id,
                b.Name,
                b.TaxIdentificationNumber.Value,
                b.InvoicePrefix,
                new AddressDto(
                    b.RegisteredAddress.Street,
                    b.RegisteredAddress.City,
                    b.RegisteredAddress.State,
                    b.RegisteredAddress.Country,
                    b.RegisteredAddress.PostalCode),
                b.Status,
                b.Users.ToList(),
                b.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PaginatedList<BusinessUsersSummaryDto>(businesses, totalCount, request.PageNumber, request.PageSize);
    }
}