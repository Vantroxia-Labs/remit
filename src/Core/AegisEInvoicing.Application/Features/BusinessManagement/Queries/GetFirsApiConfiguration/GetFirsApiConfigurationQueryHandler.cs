using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFirsApiConfiguration;

public class GetFirsApiConfigurationQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    ILogger<GetFirsApiConfigurationQueryHandler> logger) 
    : IRequestHandler<GetFirsApiConfigurationQuery, GetFirsApiConfigurationResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetFirsApiConfigurationQueryHandler> _logger = logger;
    private readonly IEncryptionService _encryptionService = encryptionService;

    public async Task<GetFirsApiConfigurationResult> Handle(GetFirsApiConfigurationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return (GetFirsApiConfigurationResult)GetFirsApiConfigurationResult.AuthorizationError();

            var businessId = _currentUser.BusinessId!.Value;

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

            if (business is null)
                return (GetFirsApiConfigurationResult)GetFirsApiConfigurationResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            if (string.IsNullOrEmpty(business.FIRSApiKey) || string.IsNullOrEmpty(business.FIRSClientSecret))
                return (GetFirsApiConfigurationResult)GetFirsApiConfigurationResult.NotFound(ResponseMessages.BUSINESS_FIRS_CREDENTIALS_NOT_CONFIGURED);

            var decryptedApiKey = await _encryptionService.DecryptAsync(business.FIRSApiKey);
            var decryptedClientSecret = await _encryptionService.DecryptAsync(business.FIRSClientSecret);

            return new GetFirsApiConfigurationResult
            {
                 FirsApiConfiguration = new FirsApiConfigurationResult(decryptedApiKey, decryptedClientSecret),
                 IsSuccess = true,
                 Message = ResponseMessages.OPERATION_SUCCESSFUL,
                 StatusCodes = HttpStatusCodes.OK.ToInt()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve FIRS API configuration for business");
            return (GetFirsApiConfigurationResult)GetFirsApiConfigurationResult.Failure();
        }
    }

    private bool IsUserAuthorized() =>
    _currentUser.UserId.HasValue && _currentUser.BusinessId.HasValue;
}
