using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.DTOs;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetBusinessStatistics;

public class GetBusinessStatisticsQueryHandler : IRequestHandler<GetBusinessStatisticsQuery, BusinessStatisticsDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetBusinessStatisticsQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<BusinessStatisticsDto> Handle(GetBusinessStatisticsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Businesses.AsQueryable();

        // Apply security filters - Business admins can only see their own business statistics
        if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
        {
            query = query.Where(b => b.Id == _currentUserService.BusinessId!.Value);
        }

        if (request.BusinessId.HasValue)
        {
            query = query.Where(b => b.Id == request.BusinessId.Value);
        }

        // Calculate statistics
        var totalBusinesses = await query.CountAsync(cancellationToken);
        var pendingBusinesses = await query.CountAsync(b => b.Status == BusinessStatus.Pending, cancellationToken);
        var activeBusinesses = await query.CountAsync(b => b.Status == BusinessStatus.Active, cancellationToken);
        var inactiveBusinesses = await query.CountAsync(b => b.Status == BusinessStatus.Inactive, cancellationToken);

        return new BusinessStatisticsDto(
            totalBusinesses,
            pendingBusinesses,
            activeBusinesses,
            inactiveBusinesses);
    }
}