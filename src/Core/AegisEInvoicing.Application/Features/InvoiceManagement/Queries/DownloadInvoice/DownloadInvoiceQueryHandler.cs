using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.DownloadInvoice;

public sealed class DownloadInvoiceQueryHandler : IRequestHandler<DownloadInvoiceQuery, DownloadInvoiceResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<DownloadInvoiceQueryHandler> _logger;
    private readonly IFIRSHttpClient _firsClient;
    private readonly IEncryptionService _encryptionService;
    public DownloadInvoiceQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<DownloadInvoiceQueryHandler> logger,
        IFIRSHttpClient firsClient,
        IEncryptionService encryptionService)
    {
        _context = context;
        _currentUser = currentUserService;
        _logger = logger;
        _firsClient = firsClient;
        _encryptionService = encryptionService;
    }

    public async Task<DownloadInvoiceResult> Handle(DownloadInvoiceQuery request, CancellationToken cancellationToken)
    {
        if (!IsUserAuthorized())
            return (DownloadInvoiceResult)DownloadInvoiceResult.AuthorizationError();

        var businessId = _currentUser.BusinessId!.Value;

        var business = await _context.Businesses
            .AsNoTracking()
            .Where(b => b.Id == businessId)
            .FirstOrDefaultAsync(cancellationToken);

        if (business is null)
            return (DownloadInvoiceResult)DownloadInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

        if (string.IsNullOrEmpty(business.FIRSApiKey) || string.IsNullOrEmpty(business.FIRSClientSecret))
            return (DownloadInvoiceResult)DownloadInvoiceResult.NotFound(ResponseMessages.BUSINESS_FIRS_CREDENTIALS_NOT_CONFIGURED);

        var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceApprovalHistory)
               .Where(i => i.Id == request.InvoiceId && i.BusinessId == businessId)
               .FirstOrDefaultAsync(cancellationToken);
        
        if(invoice is null)
        return (DownloadInvoiceResult)DownloadInvoiceResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND);

        var invoiceStatuses = invoice.InvoiceApprovalHistory.Select(stat => stat.InvoiceStatus).ToList();

        if (!invoiceStatuses.Contains(InvoiceStatus.SIGNED))
            return (DownloadInvoiceResult)DownloadInvoiceResult.BadRequest();

        var decryptedApiKey = await _encryptionService.DecryptAsync(business.FIRSApiKey);
        var decryptedClientSecret = await _encryptionService.DecryptAsync(business.FIRSClientSecret);

        var downloadedInvoice = await _firsClient.DownloadInvoiceAsync(invoice.Irn.Value, decryptedApiKey, decryptedClientSecret, cancellationToken);

        if (downloadedInvoice.Code != HttpStatusCodes.OK.ToInt())
            return (DownloadInvoiceResult)DownloadInvoiceResult.NotFound(downloadedInvoice.Error?.Details ?? ResponseMessages.INVOICE_NOT_FOUND);

        if (downloadedInvoice.Data is null)
            return (DownloadInvoiceResult)DownloadInvoiceResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND);

         var decryptInvoice = AesDecryptionServiceAlt.Decrypt(
            Encoding.UTF8.GetBytes(downloadedInvoice.Data!.Pub.Trim() + decryptedApiKey.Split('-').First()),
            AesDecryptionServiceAlt.HexStringToByteArray(downloadedInvoice.Data.IvHex.Trim()),
            downloadedInvoice.Data!.Data.Trim());

        if (string.IsNullOrEmpty(decryptInvoice))
            return (DownloadInvoiceResult)DownloadInvoiceResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND);

        return new DownloadInvoiceResult
        {
            IsSuccess = true,
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = ResponseMessages.OPERATION_SUCCESSFUL,
            InvoiceData = decryptInvoice,
            QrCode = invoice.QRCode?.GetBase64String()
        };
    }

    private bool IsUserAuthorized() =>
      _currentUser.UserId.HasValue &&
        _currentUser.BusinessId.HasValue;
}
