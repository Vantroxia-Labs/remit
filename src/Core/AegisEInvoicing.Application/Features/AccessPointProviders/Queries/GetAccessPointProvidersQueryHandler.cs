using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

public class GetAccessPointProvidersQueryHandler : IRequestHandler<GetAccessPointProvidersQuery, PaginatedList<AccessPointProvidersDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GetAccessPointProvidersQueryHandler(IApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<PaginatedList<AccessPointProvidersDto>> Handle(GetAccessPointProvidersQuery request, CancellationToken cancellationToken)
    {

        List<AccessPointProvidersDto> accessProviders = new List<AccessPointProvidersDto>();
        var query = await _context.FIRSApiConfigurations.Where(f => f.IsActive == true).ToListAsync();

        if(!query.Any())
            return new PaginatedList<AccessPointProvidersDto>(accessProviders, 0 , request.PageNumber, request.PageSize);

        var totalCount = query.Count();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Sanitize search term to prevent SQL injection (VAPT finding)
            var search = InputSanitizationService.SanitizeSearchTerm(request.SearchTerm);
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Name.ToLower().Contains(search)).ToList();
            }
        }

        accessProviders.AddRange(query.Select(f => new AccessPointProvidersDto(
            f.Id, 
            f.Name, 
            f.Description,
            _currentUser.IsPlatformAdmin ? f.EncryptedApiSecret : "************", 
            _currentUser.IsPlatformAdmin ? f.EncryptedApiKey : "************",
            f.Environment, f.BaseUrl)
        ));

        return new PaginatedList<AccessPointProvidersDto>(accessProviders, totalCount, request.PageNumber, request.PageSize);
    }
}
