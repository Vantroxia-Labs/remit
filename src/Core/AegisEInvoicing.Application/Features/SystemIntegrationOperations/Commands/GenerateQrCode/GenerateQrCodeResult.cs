using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.GenerateQrCode;

public record GenerateQrCodeResult : GenericResult
{
    public QRCode? QRCode { get; private set; } = null;

    public new static GenerateQrCodeResult NotFound(string message)
    {
        return new GenerateQrCodeResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.NotFound.ToInt(),
            Message = message
        };
    }

    public new static GenerateQrCodeResult AuthorizationError(string? message = null)
    {
        return new GenerateQrCodeResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.Forbidden.ToInt(),
            Message = message ?? ResponseMessages.INSUFFICIENT_PERMISSIONS
        };
    }

    public static GenerateQrCodeResult Successful(QRCode qRCode)
    {
        return new GenerateQrCodeResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.GENERATE_QR_CODE_SUCCESS,
            QRCode = qRCode
        };
    }

    public new static GenerateQrCodeResult Failure(string? message = null)
    {
        return new GenerateQrCodeResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.ExpectationFailed.ToInt(),
            Message = message ?? ResponseMessages.GENERATE_QR_CODE_FAILED
        };
    }
}
