using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.GenerateQrCode;

public class GenerateQrCodeCommandHandler(
    IApplicationDbContext context,
    IHttpContextAccessor currentUser,
    ILogger<GenerateQrCodeCommandHandler> logger)
    : IRequestHandler<GenerateQrCodeCommand, GenerateQrCodeResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _currentUser = currentUser;
    private readonly ILogger<GenerateQrCodeCommandHandler> _logger = logger;

    public async Task<GenerateQrCodeResult> Handle(GenerateQrCodeCommand request, CancellationToken cancellationToken)
    {
        try
        {

            var businessId = TryGetBusinessId();
            if (businessId is null)
                return GenerateQrCodeResult.AuthorizationError();

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null)
                return GenerateQrCodeResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            if (string.IsNullOrEmpty(business.Certificate) ||
               string.IsNullOrEmpty(business.PublicKey))
                return GenerateQrCodeResult.NotFound(ResponseMessages.BUSINESS_QR_CODE_KEYS_NOT_CONFIGURED);

            var qrCode = InvoiceQrService.GenerateQrCode(
                                request.Irn,
                                business.Certificate!,
                                business.PublicKey!);

            if (qrCode is null)
                return GenerateQrCodeResult.Failure();

            _logger.LogInformation("System Integrator QRCode for business, {businessName}, with Id, {businessId} successfully generated", business.Name, business.Id);
            return GenerateQrCodeResult.Successful(QRCode.Create(qrCode, []));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Generate QRCode");
            return GenerateQrCodeResult.Failure(ResponseMessages.GENERATE_IRN_FAILED);
        }
    }

    private Guid? TryGetBusinessId()
    {
        var businessId = _currentUser.HttpContext?.User?.FindFirst("BusinessId")?.Value;
        return Guid.TryParse(businessId, out var result) ? result : null;
    }
}
