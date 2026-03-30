using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Common.Security;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetAllBusinessFIRSConfigurations;

public class GetAllBusinessFIRSConfigurationsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetAllBusinessFIRSConfigurationsQueryHandler> logger) : IRequestHandler<GetAllBusinessFIRSConfigurationsQuery, PaginatedList<BusinessFIRSApiConfigurationDto>>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ILogger<GetAllBusinessFIRSConfigurationsQueryHandler> _logger = logger;

    public async Task<PaginatedList<BusinessFIRSApiConfigurationDto>> Handle(GetAllBusinessFIRSConfigurationsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var query = _context.BusinessFIRSApiConfigurations
                .Include(bf => bf.Business)
                .Include(bf => bf.FIRSApiConfiguration)
                .Where(bf => !bf.IsDeleted)
                .AsQueryable();

            // Apply filters with sanitized input to prevent SQL injection (VAPT finding)
            if (!string.IsNullOrWhiteSpace(request.BusinessName))
            {
                var businessNameFilter = InputSanitizationService.SanitizeSearchTerm(request.BusinessName);
                if (!string.IsNullOrEmpty(businessNameFilter))
                {
                    query = query.Where(bf => bf.Business.Name.ToLower().Contains(businessNameFilter));
                }
            }

            if (!string.IsNullOrWhiteSpace(request.ConfigurationName))
            {
                var configNameFilter = InputSanitizationService.SanitizeSearchTerm(request.ConfigurationName);
                if (!string.IsNullOrEmpty(configNameFilter))
                {
                    query = query.Where(bf => bf.FIRSApiConfiguration.Name.ToLower().Contains(configNameFilter));
                }
            }

            var mappedQuery = query.Select(bf => new BusinessFIRSApiConfigurationDto
            {
                Id = bf.Id,
                BusinessId = bf.BusinessId,
                FIRSApiConfigurationId = bf.FIRSApiConfigurationId,
                BusinessName = bf.Business.Name,
                ConfigurationName = bf.FIRSApiConfiguration.Name,
                ConfigurationDescription = bf.FIRSApiConfiguration.Description,
                DeploymentType = bf.FIRSApiConfiguration.DeploymentType,
                IsActive = bf.FIRSApiConfiguration.IsActive,
                CreatedAt = bf.CreatedAt,
                UpdatedAt = bf.UpdatedAt
            })
            .OrderByDescending(bf => bf.CreatedAt);

            // Create paginated result
            var totalCount = await query.CountAsync(cancellationToken);
            var items = await mappedQuery
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var paginatedList = new PaginatedList<BusinessFIRSApiConfigurationDto>(
                items, totalCount, request.PageNumber, request.PageSize);

            _logger.LogInformation("Retrieved {Count} Business FIRS API Configurations", paginatedList.Items.Count);

            return paginatedList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Business FIRS API Configurations");
            throw;
        }
    }
}