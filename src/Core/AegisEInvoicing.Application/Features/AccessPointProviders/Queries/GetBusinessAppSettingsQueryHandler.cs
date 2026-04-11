using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

public class GetBusinessAppSettingsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<GetBusinessAppSettingsQuery, BusinessAppSettingsDto?>
{
    public async Task<BusinessAppSettingsDto?> Handle(
        GetBusinessAppSettingsQuery request,
        CancellationToken cancellationToken)
    {
        // ClientAdmin can only read their own business
        var businessId = currentUser.IsPlatformAdmin
            ? request.BusinessId
            : currentUser.BusinessId ?? Guid.Empty;

        if (businessId == Guid.Empty || (!currentUser.IsPlatformAdmin && businessId != request.BusinessId))
            return null;

        var settings = await context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId && !b.IsDeleted)
            .Select(b => new { b.ActiveVendor, b.AppEnvironmentMode })
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
            return null;

        return new BusinessAppSettingsDto(settings.ActiveVendor, settings.AppEnvironmentMode);
    }
}
