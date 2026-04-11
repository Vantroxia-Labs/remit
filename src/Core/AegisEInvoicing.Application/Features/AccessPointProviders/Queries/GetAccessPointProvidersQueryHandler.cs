using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

public class GetAccessPointProvidersQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<GetAccessPointProvidersQuery, PaginatedList<AccessPointProvidersDto>>
{
    private const string Masked = "************";

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
            {
                query = query.Where(p =>
                    p.ProviderCode.Contains(search) ||
                    p.DisplayName.ToLower().Contains(search));
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(p => p.DisplayName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Credentials are only shown in plaintext (well, still encrypted) to platform admins.
        // Non-admins see masked values.
        var isAdmin = currentUser.IsPlatformAdmin;

        var dtos = items.Select(p => new AccessPointProvidersDto(
            p.Id,
            p.ProviderCode,
            p.DisplayName,
            p.Description,
            p.AuthScheme,
            p.ApiKeyHeaderName,
            p.SignatureHeaderName,
            // Sandbox
            p.SandboxBaseUrl,
            isAdmin ? (p.EncryptedSandboxApiKey ?? string.Empty) : Masked,
            isAdmin ? (p.EncryptedSandboxApiSecret ?? string.Empty) : Masked,
            p.SandboxTokenEndpoint,
            // Production
            p.ProductionBaseUrl,
            isAdmin ? (p.EncryptedProductionApiKey ?? string.Empty) : Masked,
            isAdmin ? (p.EncryptedProductionApiSecret ?? string.Empty) : Masked,
            p.ProductionTokenEndpoint,
            p.IsActive,
            p.CreatedAt
        )).ToList();

        return new PaginatedList<AccessPointProvidersDto>(dtos, totalCount, request.PageNumber, request.PageSize);
    }
}
