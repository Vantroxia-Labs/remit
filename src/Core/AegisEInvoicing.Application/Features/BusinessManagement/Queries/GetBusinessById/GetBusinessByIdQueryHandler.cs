using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;

namespace AegisEInvoicing.Application.Features.BusinessOnboarding.Queries.GetBusinessById;

public class GetBusinessByIdQueryHandler : IRequestHandler<GetBusinessByIdQuery, BusinessDetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessByIdQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BusinessDetailDto?> Handle(GetBusinessByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Businesses.AsQueryable();

        // Apply security filters and business ID filtering
        if (_currentUserService.IsPlatformAdmin)
        {
            // Platform admin: can query specific business or return null if no business ID provided
            if (request.BusinessId.HasValue)
            {
                query = query.Where(b => b.Id == request.BusinessId.Value);
            }
            else
            {
                // No business ID provided - return null (will be handled as not found)
                return null;
            }
        }
        else
        {
            // Non-platform admin: can only see their own business
            if (!_currentUserService.BusinessId.HasValue)
            {
                throw new UnauthorizedAccessException("User must be associated with a business");
            }
            query = query.Where(b => b.Id == _currentUserService.BusinessId.Value);
        }
        
        var business = await query
                .Include(b => b.Subscriptions)
                .ThenInclude(s => s.PlatformSubscription)
                .Include(b => b.Users)
                .SingleOrDefaultAsync(cancellationToken);

        return business is null
           ? throw new NotFoundException("Business Not Found")
           : new BusinessDetailDto
           {
               Id = business.Id,
               Name = business.Name,
               Description = business.Description,
               Industry = business.Industry,
               FIRSBusinessId = business.FIRSBusinessId,
               ServiceId = business.ServiceId,
               InvoicePrefix = business.InvoicePrefix,
               RegisteredAddress = new AddressDto(
                   business.RegisteredAddress.Street,
                   business.RegisteredAddress.City,
                   business.RegisteredAddress.State,
                   business.RegisteredAddress.Country,
                   business.RegisteredAddress.PostalCode),
               BusinessRegistrationNumber = business.BusinessRegistrationNumber,
               TIN = business.TaxIdentificationNumber.Value,
               ContactEmail = business.ContactEmail,
               ContactPhone = business.ContactPhone,
               Status = business.Status,
               CreatedAt = business.CreatedAt,
               SubscriptionInfo = business.GetPrimarySubscription() is { } ps
                                      ? new BusinessSubscriptionDto(ps.PlatformSubscription.PlanName,
                                                                    ps.PlatformSubscription.MonthlyPrice,
                                                                    ps.Status,
                                                                    ps.StartDate,
                                                                    ps.EndDate,
                                                                    ps.NextBillingDate)
                                      : null,
               UserCount = business.Users.Count
           };
    }
}