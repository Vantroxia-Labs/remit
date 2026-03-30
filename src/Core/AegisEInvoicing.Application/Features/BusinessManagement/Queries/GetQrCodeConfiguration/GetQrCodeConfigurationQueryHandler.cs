using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetQrCodeConfiguration;

public class GetQrCodeConfigurationQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<GetQrCodeConfigurationQueryHandler> logger)
    : IRequestHandler<GetQrCodeConfigurationQuery, GetQrCodeConfigurationResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<GetQrCodeConfigurationQueryHandler> _logger = logger;

    public async Task<GetQrCodeConfigurationResult> Handle(GetQrCodeConfigurationQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return (GetQrCodeConfigurationResult)GetQrCodeConfigurationResult.AuthorizationError();

            var businessId = _currentUser.BusinessId!.Value;

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

            if (business is null)
                return (GetQrCodeConfigurationResult)GetQrCodeConfigurationResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            if (string.IsNullOrEmpty(business.PublicKey) || string.IsNullOrEmpty(business.Certificate))
                return (GetQrCodeConfigurationResult)GetQrCodeConfigurationResult.NotFound(ResponseMessages.BUSINESS_QR_CODE_KEYS_NOT_CONFIGURED);

            return (GetQrCodeConfigurationResult)GetQrCodeConfigurationResult.Successful(ResponseMessages.BUSINESS_QR_CODE_KEYS_CONFIGURED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve QR Code Keys configuration for business");
            return (GetQrCodeConfigurationResult)GetQrCodeConfigurationResult.Failure();
        }
    }

    private bool IsUserAuthorized() =>
    _currentUser.UserId.HasValue && _currentUser.BusinessId.HasValue;
}
