using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetApiCredentials;

public class GetApiCredentialsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IConfiguration configuration,
    ILogger<GetApiCredentialsQueryHandler> logger) : IRequestHandler<GetApiCredentialsQuery, GetApiCredentialsResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<GetApiCredentialsQueryHandler> _logger = logger;

    public async Task<GetApiCredentialsResult> Handle(GetApiCredentialsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUserService.UserId.HasValue || !_currentUserService.BusinessId.HasValue)
                return (GetApiCredentialsResult)GenericResult.AuthorizationError();

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == _currentUserService.BusinessId.Value, cancellationToken);

            if (business is null)
                return GetApiCredentialsResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var baseUrl = _configuration["ErpApi:BaseUrl"]
                ?? _configuration["App:BaseUrl"]
                ?? string.Empty;

            var requiredHeaders = new List<ApiRequiredHeaderDto>
            {
                new("X-API-Key", "<your-api-key>", "Your API key generated from the settings panel."),
                new("Content-Type", "application/json", "Required only for POST/PUT/PATCH requests that include a JSON request body."),
                new("X-Request-Nonce", "<unique-random-value>", "Required for replay-protected critical write endpoints."),
                new("X-Request-Timestamp", "<UTC ISO-8601 timestamp>", "Required for replay-protected critical write endpoints. Example: 2026-04-12T10:15:30.000Z")
            };

            return new GetApiCredentialsResult
            {
                IsSuccess = true,
                StatusCodes = HttpStatusCodes.OK.ToInt(),
                Message = ResponseMessages.OPERATION_SUCCESSFUL,
                Credentials = new ApiCredentialsDto(
                    business.ApiKey ?? string.Empty,
                    business.IsApiKeyActive,
                    baseUrl,
                    requiredHeaders,
                    business.ApiKeyGeneratedAt,
                    business.ApiKeyLastUsedAt)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch API credentials for business {BusinessId}", _currentUserService.BusinessId);
            return (GetApiCredentialsResult)GenericResult.Failure();
        }
    }

}
