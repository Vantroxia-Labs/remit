using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.RotateApiKey;

public class RotateApiKeyCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ITotpService totpService,
    ILogger<RotateApiKeyCommandHandler> logger) : IRequestHandler<RotateApiKeyCommand, RotateApiKeyResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ITotpService _totpService = totpService;
    private readonly ILogger<RotateApiKeyCommandHandler> _logger = logger;

    public async Task<RotateApiKeyResult> Handle(RotateApiKeyCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue || !_currentUserService.BusinessId.HasValue)
                return (RotateApiKeyResult)GenericResult.AuthorizationError();

            if (!int.TryParse(request.Otp, out var otpCode))
                return RotateApiKeyResult.BadRequest("Invalid OTP format.");

            var isOtpValid = _totpService.Verify(otpCode, $"{_currentUserService.UserId.Value}");
            if (!isOtpValid)
                return RotateApiKeyResult.BadRequest("Invalid or expired OTP.");

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == _currentUserService.BusinessId.Value, cancellationToken);

            if (business is null)
                return new RotateApiKeyResult
                {
                    IsSuccess = false,
                    StatusCodes = HttpStatusCodes.NotFound.ToInt(),
                    Message = ResponseMessages.BUSINESS_NOT_FOUND
                };

            if (business.IsApiKeyActive && !string.IsNullOrWhiteSpace(business.ApiKey))
                business.RevokeApiKey(_currentUserService.UserId.Value);

            var newApiKey = $"aeg-{Guid.NewGuid():N}";
            business.SetApiKey(newApiKey, _currentUserService.UserId.Value);

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("API key rotated successfully for business {BusinessId}", business.Id);

            return new RotateApiKeyResult
            {
                IsSuccess = true,
                StatusCodes = HttpStatusCodes.OK.ToInt(),
                Message = "API key rotated successfully.",
                NewApiKey = newApiKey
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rotate API key for business {BusinessId}", _currentUserService.BusinessId);
            return (RotateApiKeyResult)GenericResult.Failure();
        }
    }
}
