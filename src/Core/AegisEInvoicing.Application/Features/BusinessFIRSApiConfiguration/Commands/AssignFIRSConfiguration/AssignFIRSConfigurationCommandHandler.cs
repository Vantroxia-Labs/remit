using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Commands.AssignFIRSConfiguration;

public class AssignFIRSConfigurationCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<AssignFIRSConfigurationCommandHandler> logger) : IRequestHandler<AssignFIRSConfigurationCommand, AssignFIRSConfigurationResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<AssignFIRSConfigurationCommandHandler> _logger = logger;

    public async Task<AssignFIRSConfigurationResult> Handle(AssignFIRSConfigurationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue)
                throw new AuthenticationException("User authentication required");

            if (!_currentUser.BusinessId.HasValue)
                throw new NotFoundException("Business Id required");

            // Check if business exists
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == _currentUser.BusinessId.Value, cancellationToken) ?? throw new NotFoundException("Business not found");

            // Check if FIRS configuration exists
            var firsConfig = await _context.FIRSApiConfigurations
                .Where(f => f.Id == request.FIRSApiConfigurationId)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken) ?? throw new NotFoundException("FIRS API Configuration not found");
            
            if (!firsConfig.IsActive)
                throw new BadRequestException("FIRS API Configuration is not active");

            if(firsConfig is null)
                    throw new NotFoundException("FIRS API Configuration not Found");

            // Check if business already has a configuration assigned
            var existingConfig = await _context.BusinessFIRSApiConfigurations
                .FirstOrDefaultAsync(bf => bf.BusinessId == _currentUser.BusinessId.Value && !bf.IsDeleted, cancellationToken);

            if (existingConfig is not null)
            {
                // Update existing configuration
                existingConfig.Update(firsConfig.Id);
                _context.BusinessFIRSApiConfigurations.Update(existingConfig);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Updated FIRS API Configuration for Business {BusinessId} to Configuration {ConfigurationId}",
                    _currentUser.BusinessId.Value, request.FIRSApiConfigurationId);
            }
            else
            {
                var businessFIRSConfig = Domain.Entities.BusinessManagement.BusinessFIRSApiConfiguration.Create(
                    _currentUser.BusinessId.Value,
                    firsConfig.Id);

                _context.BusinessFIRSApiConfigurations.Add(businessFIRSConfig);
                await _context.SaveChangesAsync(cancellationToken);

                business.AssignFirsApiConfiguration(businessFIRSConfig.Id, _currentUser.UserId.Value);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Assigned FIRS API Configuration {ConfigurationId} to Business {BusinessId}",
                    request.FIRSApiConfigurationId, _currentUser.BusinessId.Value);
            }

            return new AssignFIRSConfigurationResult(
                true,
                $"{firsConfig.Name} Provider assigned successfully to {business.Name}",
                existingConfig?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning FIRS API Configuration to Business");
            throw new UnprocessableEntityException("An error occurred while assigning FIRS API Configuration");
        }
    }
}