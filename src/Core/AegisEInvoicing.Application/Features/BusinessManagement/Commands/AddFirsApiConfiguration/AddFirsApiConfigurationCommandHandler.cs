using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.AddFirsApiConfiguration;

public class AddFirsApiConfigurationCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    ILogger<AddFirsApiConfigurationCommandHandler> logger) 
    : IRequestHandler<AddFirsApiConfigurationCommand, GenericResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<AddFirsApiConfigurationCommandHandler> _logger = logger;
    private readonly IEncryptionService _encryptionService = encryptionService;

    public async Task<GenericResult> Handle(AddFirsApiConfigurationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return GenericResult.AuthorizationError();

            var businessId = _currentUser.BusinessId!.Value;

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null)
                return GenericResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var encryptedApiKey = await _encryptionService.EncryptAsync(request.FirsApiKey);
            var encryptedClientSecret = await _encryptionService.EncryptAsync(request.FirsClientSecret);

            business.AddFirsConfiguration(encryptedApiKey, encryptedClientSecret);

            await _context.SaveChangesAsync(cancellationToken);

            return GenericResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add FIRS API configuration for business");
            return GenericResult.Failure();
        }
    }

    private bool IsUserAuthorized() =>
     _currentUser.UserId.HasValue && _currentUser.BusinessId.HasValue;
}