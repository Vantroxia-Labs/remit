using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ImportFirsInvoices;

namespace AegisEInvoicing.Application.Common.Interfaces;

public interface IFirsMbsApiClient
{
    Task<string> LoginAsync(string email, string password, CancellationToken cancellationToken);
    Task<MbsInvoiceListData?> GetInvoicePageAsync(string token, int page, int size, CancellationToken cancellationToken);
    Task<MbsInvoiceDetail?> GetInvoiceDetailAsync(string token, string irn, CancellationToken cancellationToken);
}
