using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

public class GetAccessPointProvidersQueryHandler(
    IApplicationDbContext context,
    IEnumerable<IAccessPointProviderClient> adapters)
    : IRequestHandler<GetAccessPointProvidersQuery, PaginatedList<AccessPointProvidersDto>>
{
    // Build a display-name lookup from registered adapters so the list endpoint
    // can return human-readable names without any enum or hardcoded table.
    private readonly Dictionary<string, string> _displayNames =
        adapters.ToDictionary(a => a.ProviderCode, a => a.DisplayName, StringComparer.OrdinalIgnoreCase);

    public async Task<PaginatedList<AccessPointProvidersDto>> Handle(
        GetAccessPointProvidersQuery request,
        CancellationToken cancellationToken)
    {
        var query = context.AppProviderConfigurations
            .AsNoTracking()
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = InputSanitizationService.SanitizeSearchTerm(request.SearchTerm);
            if (!string.IsNullOrEmpty(search))
                query = query.Where(p => p.Name.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.AdapterKey)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = items.Select(p => new AccessPointProvidersDto(
            p.Id,
            p.Name,
            p.Description,
            p.AdapterKey,
            DisplayName: _displayNames.TryGetValue(p.AdapterKey, out var name) ? name : p.AdapterKey,
            p.BaseUrl,
            HasProductionCredentials: p.EncryptedCredentials is not null,
            p.SandboxBaseUrl,
            HasSandboxCredentials: p.EncryptedSandboxCredentials is not null,
            p.IsActive,
            p.CreatedAt
        )).ToList();

        return new PaginatedList<AccessPointProvidersDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
