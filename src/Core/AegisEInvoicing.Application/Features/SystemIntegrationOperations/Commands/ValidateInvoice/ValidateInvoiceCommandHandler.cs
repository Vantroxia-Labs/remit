using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.ValidateInvoice;

public class ValidateInvoiceCommandHandler(
    IApplicationDbContext context,
    IHttpContextAccessor currentUser,
    IEncryptionService encryptionService,
    IFIRSHttpClient firsHttpClient,
    ILogger<ValidateInvoiceCommandHandler> logger)
    : IRequestHandler<ValidateInvoiceCommand, ValidateInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _currentUser = currentUser;
    private readonly ILogger<ValidateInvoiceCommandHandler> _logger = logger;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IFIRSHttpClient _firshttpClient = firsHttpClient;

    public async Task<ValidateInvoiceResult> Handle(ValidateInvoiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var businessId = TryGetBusinessId();
            if (businessId is null)
                return ValidateInvoiceResult.AuthorizationError();

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null)
                return ValidateInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);            

            if (string.IsNullOrEmpty(business.FIRSApiKey) || string.IsNullOrEmpty(business.FIRSClientSecret))
                return ValidateInvoiceResult.NotFound(ResponseMessages.BUSINESS_FIRS_CREDENTIALS_NOT_CONFIGURED);

            var decryptedApiKey = await _encryptionService.DecryptAsync(business.FIRSApiKey);
            var decryptedClientSecret = await _encryptionService.DecryptAsync(business.FIRSClientSecret);

            var response = await _firshttpClient.ValidateInvoiceDataAsync(request.ValidateInvoiceData, decryptedApiKey, decryptedClientSecret, cancellationToken);

            if (response.Code == HttpStatusCodes.OK.ToInt() || response.Code == 0 || (response.Data?.Ok ?? false))
            {
                _logger.LogInformation("System Integrator Invoice for business, {buisnessName}, with Id, {businessId} successfully validated by FIRS", business.Name, business.Id);
                return ValidateInvoiceResult.Successful();
            }
            else
            {
                _logger.LogWarning("System Integrator Invoice for business, {buisnessName}, with Id, {businessId} validation failed FIRS validation with code: {Code}", business.Name, business.Id, response.Code);
                return new ValidateInvoiceResult
                {
                    IsSuccess = false,
                    StatusCodes = response.Code,
                    Message = !string.IsNullOrWhiteSpace(response.Error?.Details)
                              ? $"FIRS Response: {response.Error.Details}"
                              : !string.IsNullOrWhiteSpace(response.Error?.PublicMessage)
                                  ? $"FIRS Response: {response.Error.PublicMessage}"
                                  : ResponseMessages.INVOICE_VALIDATION_FAILED
                };
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate System Integrator invoice");
            return ValidateInvoiceResult.Failure(ResponseMessages.INVOICE_VALIDATION_FAILED);
        }
    }
    private Guid? TryGetBusinessId()
    {
        var businessId = _currentUser.HttpContext?.User?.FindFirst("BusinessId")?.Value;
        return Guid.TryParse(businessId, out var result) ? result : null;
    }
}