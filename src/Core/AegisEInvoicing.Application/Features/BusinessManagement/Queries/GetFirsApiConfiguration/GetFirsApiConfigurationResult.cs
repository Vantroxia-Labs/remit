using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetFirsApiConfiguration;

public record GetFirsApiConfigurationResult : GenericResult
{
    public FirsApiConfigurationResult? FirsApiConfiguration { get; set; } = null;

    public new static GetFirsApiConfigurationResult NotFound(string message)
    {
        return new GetFirsApiConfigurationResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }
}

public record FirsApiConfigurationResult(
    string? FirsApiKey,
    string? FirsClientSecret);
