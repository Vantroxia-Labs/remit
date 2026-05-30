using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetApiCredentials;

public record GetApiCredentialsResult : GenericResult
{
    public ApiCredentialsDto? Credentials { get; set; }

    public new static GetApiCredentialsResult NotFound(string message)
    {
        return new GetApiCredentialsResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }
}

public record ApiCredentialsDto(
    string ApiKey,
    bool IsApiKeyActive,
    string BaseUrl,
    IEnumerable<ApiRequiredHeaderDto> RequiredHeaders,
    DateTimeOffset? ApiKeyGeneratedAt,
    DateTimeOffset? ApiKeyLastUsedAt);

public record ApiRequiredHeaderDto(
    string Name,
    string Value,
    string Description);
