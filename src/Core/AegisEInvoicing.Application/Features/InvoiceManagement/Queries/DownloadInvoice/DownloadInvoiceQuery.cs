using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.DownloadInvoice;

public record DownloadInvoiceQuery(
        Guid InvoiceId) : IRequest<DownloadInvoiceResult>;

public record DownloadInvoiceResult : GenericResult
{
  public string? InvoiceData { get; set; }
  public string? QrCode { get; set; }

    public static DownloadInvoiceResult BadRequest()
    {
        return new DownloadInvoiceResult
        {
            IsSuccess = false,
            StatusCodes = HttpStatusCodes.BadRequest.ToInt(),
            Message = ResponseMessages.INVOICE_NOT_SIGNED
        };
    }
}