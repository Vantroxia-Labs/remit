using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.AddQrCodeConfiguration;

public class AddQrCodeConfigurationCommandHandler(IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<AddQrCodeConfigurationCommandHandler> logger)
    : IRequestHandler<AddQrCodeConfigurationCommand, GenericResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<AddQrCodeConfigurationCommandHandler> _logger = logger;

    public async Task<GenericResult> Handle(AddQrCodeConfigurationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return GenericResult.AuthorizationError();

            var businessId = _currentUser.BusinessId!.Value;

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null)
                return GenericResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            business.SetQrCodeKeys(request.PublicKey, request.Certificate);
            await _context.SaveChangesAsync(cancellationToken);

            return GenericResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add QR Code configuration for business");
            return GenericResult.Failure();
        }
    }

    private bool IsUserAuthorized() =>
     _currentUser.UserId.HasValue && _currentUser.BusinessId.HasValue;
}
