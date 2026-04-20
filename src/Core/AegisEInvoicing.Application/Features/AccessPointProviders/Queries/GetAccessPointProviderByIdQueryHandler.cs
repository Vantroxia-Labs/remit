using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

public class GetAccessPointProviderByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    IEnumerable<IAccessPointProviderClient> adapters)
    : IRequestHandler<GetAccessPointProviderByIdQuery, AccessPointProviderEditDto?>
{
    private readonly Dictionary<string, string> _displayNames =
        adapters.ToDictionary(a => a.ProviderCode, a => a.DisplayName, StringComparer.OrdinalIgnoreCase);

    public async Task<AccessPointProviderEditDto?> Handle(
        GetAccessPointProviderByIdQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsPlatformAdmin)
            return null;

        var p = await context.AppProviderConfigurations
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (p is null)
            return null;

        var credentials = p.EncryptedCredentials is not null
            ? await encryptionService.DecryptAsync(p.EncryptedCredentials)
            : null;

        var sandboxCredentials = p.EncryptedSandboxCredentials is not null
            ? await encryptionService.DecryptAsync(p.EncryptedSandboxCredentials)
            : null;

        var displayName = _displayNames.TryGetValue(p.AdapterKey, out var name) ? name : p.AdapterKey;

        return new AccessPointProviderEditDto(
            p.Id,
            p.Name,
            p.Description,
            p.AdapterKey,
            displayName,
            p.BaseUrl,
            credentials,
            p.SandboxBaseUrl,
            sandboxCredentials,
            p.IsActive,
            p.CreatedAt);
    }
}
