using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Queries.GetBusinessFIRSConfiguration;

public class GetBusinessFIRSConfigurationQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetBusinessFIRSConfigurationQueryHandler> logger) : IRequestHandler<GetBusinessFIRSConfigurationQuery, BusinessFIRSApiConfigurationDetailDto?>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetBusinessFIRSConfigurationQueryHandler> _logger = logger;

    public async Task<BusinessFIRSApiConfigurationDetailDto?> Handle(GetBusinessFIRSConfigurationQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            throw new AuthenticationException("User authentication required");

        if (!_currentUser.BusinessId.HasValue)
            throw new NotFoundException("Business Id required");

        var configuration = await _context.BusinessFIRSApiConfigurations
                        .Include(bf => bf.Business)
                        .Include(bf => bf.FIRSApiConfiguration)
                        .Where(bf => bf.BusinessId == _currentUser.BusinessId.Value && !bf.IsDeleted)
                        .Select(bf => new BusinessFIRSApiConfigurationDetailDto
                        {
                            ConfigurationName = bf.FIRSApiConfiguration.Name,
                            ConfigurationDescription = bf.FIRSApiConfiguration.Description
                        })
                        .FirstOrDefaultAsync(cancellationToken);

        if (configuration is null)
        {
            _logger.LogInformation("No FIRS API Configuration found for Business {BusinessId}", _currentUser.BusinessId);
            throw new NotFoundException("No Access Point Provider is assigned to this Business");
        }

        return configuration;
    }
}