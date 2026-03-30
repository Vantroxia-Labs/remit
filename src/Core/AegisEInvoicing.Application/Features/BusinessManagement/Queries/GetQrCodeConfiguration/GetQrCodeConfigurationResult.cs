using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetQrCodeConfiguration;

public record GetQrCodeConfigurationResult : GenericResult
{
    public static GetQrCodeConfigurationResult Successful(string message)
    {
        return new GetQrCodeConfigurationResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.BUSINESS_QR_CODE_KEYS_CONFIGURED
        };
    }
    public new static GetQrCodeConfigurationResult NotFound(string message)
    {
        return new GetQrCodeConfigurationResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }
}