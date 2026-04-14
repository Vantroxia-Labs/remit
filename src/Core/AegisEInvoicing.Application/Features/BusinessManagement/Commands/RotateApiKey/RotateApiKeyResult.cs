using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.RotateApiKey;

public record RotateApiKeyResult : GenericResult
{
    public string? NewApiKey { get; init; }

    public new static RotateApiKeyResult BadRequest(string message)
    {
        return new RotateApiKeyResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = message
        };
    }
}
