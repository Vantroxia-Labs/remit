using AegisEInvoicing.Application.Common.Extensions;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.FIRSAccessPoint;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoiceByIrn;

public class ValidateInvoiceByIrnCommandHandler(
 IApplicationDbContext context,
    ICurrentUserService currentUser,
    IEncryptionService encryptionService,
    IFIRSHttpClient firsHttpClient,
    ILogger<ValidateInvoiceByIrnCommandHandler> logger)
    : IRequestHandler<ValidateInvoiceByIrnCommand, ValidateInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ValidateInvoiceByIrnCommandHandler> _logger = logger;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly IFIRSHttpClient _firshttpClient = firsHttpClient;

    public async Task<ValidateInvoiceResult> Handle(ValidateInvoiceByIrnCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return (ValidateInvoiceResult)ValidateInvoiceResult.AuthorizationError();

            var businessId = _currentUser.BusinessId!.Value;

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null)
                return ValidateInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var invoice = await _context.Invoices
                .Include(i => i.Business)
                .Include(i => i.Party)
                .Include(i => i.InvoiceLine)
                .ThenInclude(il => il.BusinessItem)
                .Include(i => i.BillingReferences)
                .Where(i => i.Irn == IRN.CreateFromString(request.Irn)
                         && (i.InvoiceStatus == InvoiceStatus.APPROVED || i.InvoiceStatus == InvoiceStatus.VALIDATIONFAILED)
                         && i.BusinessId == businessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
                return ValidateInvoiceResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND);

            if (string.IsNullOrEmpty(invoice.Business.FIRSApiKey) || string.IsNullOrEmpty(invoice.Business.FIRSClientSecret))
                return ValidateInvoiceResult.NotFound(ResponseMessages.BUSINESS_FIRS_CREDENTIALS_NOT_CONFIGURED);

            var decryptedApiKey = await _encryptionService.DecryptAsync(invoice.Business.FIRSApiKey);
            var decryptedClientSecret = await _encryptionService.DecryptAsync(invoice.Business.FIRSClientSecret);

            var validationRequest = new ValidateInvoiceDataRequest
            {
                BusinessId = invoice.Business.FIRSBusinessId.ToString(),
                Irn = invoice.Irn?.Value ?? string.Empty,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                IssueTime = invoice.IssueTime,
                InvoiceTypeCode = invoice.InvoiceType.Code.ToString(),
                PaymentStatus = invoice.PaymentStatus.GetDisplayName(),
                Note = invoice.Note,
                DocumentCurrencyCode = invoice.Currency.Code,
                TaxCurrencyCode = invoice.Currency.Code,
                InvoiceDeliveryPeriod = invoice.DeliveryPeriod.ToInvoiceDeliveryPeriod(),
                AccountingCustomerParty = invoice.Party.ToAccountingCustomerParty(),
                AccountingSupplierParty = invoice.Business.ToAccountingSupplierParty(),
                BillingReference = invoice.BillingReferences.ToList().ToBillingReference(),
                DocumentReference = invoice.AdditionalDocumentReferences.ToList().ToAddtionalDocumentReference(),
                DispatchDocumentReference = invoice.DispatchDocumentReference!.ToDispatchDocumentReference(),
                ReceiptDocumentReference = invoice.ReceiptDocumentReference!.ToReceiptDocumentReference(),
                OriginatorDocumentReference = invoice.OriginatorDocumentReference!.ToOriginatorDocumentReference(),
                ContractDocumentReference = invoice.ContractDocumentReference!.ToContractDocumentReference(),
                PaymentMeans = invoice.PaymentMeans!.ToPaymentMeans(invoice.IssueDate.AddDays(7)),
                PaymentTermsNote = invoice.PaymentTerms,
                AllowanceCharge = invoice.InvoiceLine.ToList().ToAllowanceCharge(),
                TaxTotal = invoice.InvoiceLine.ToList().ToTaxTotal(),
                LegalMonetaryTotal = invoice.InvoiceLine.ToList().ToLegalMonetaryTotal(),
                InvoiceLine = invoice.InvoiceLine.ToList().ToInvoiceLine(invoice.Currency.Code)
            };

            var response = await _firshttpClient.ValidateInvoiceDataAsync(validationRequest, decryptedApiKey, decryptedClientSecret, cancellationToken);

            if (response.Code == HttpStatusCodes.OK.ToInt() || response.Code == 0 || (response.Data?.Ok ?? false))
            {
                invoice.SetFIRSSubmissionResponseMessage("Invoice Validated");
                invoice.UpdateStatus(InvoiceStatus.VALIDATED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, ResponseMessages.INVOICE_VALIDATION_SUCCESSFUL));
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Invoice {InvoiceId} successfully validated by FIRS", invoice.Id);
            }
            else
            {
                _logger.LogWarning("Invoice {InvoiceId} validation failed with code: {Code}",
                    invoice.Id, response.Code);

                invoice.SetFIRSSubmissionResponseMessage(response.Error?.PublicMessage);
                invoice.UpdateStatus(InvoiceStatus.VALIDATIONFAILED);
                _context.InvoiceApprovalHistories.Add(InvoiceApprovalHistory.Create(invoice.Id, invoice.InvoiceStatus, response.Error?.PublicMessage ?? response.Error?.Details ?? ResponseMessages.INVOICE_VALIDATION_FAILED));
                await _context.SaveChangesAsync(cancellationToken);
                return new ValidateInvoiceResult
                {
                    IsSuccess = false,
                    StatusCodes = response.Code,
                    Message = !string.IsNullOrWhiteSpace(response.Error?.Details)
                              ? $"FIRS Response: {response.Error.Details}"
                              : !string.IsNullOrWhiteSpace(response.Error?.PublicMessage)
                                  ? $"FIRS Response: {response.Error.PublicMessage}"
                                  : ResponseMessages.INVOICE_VALIDATION_FAILED
                };
            }

            return ValidateInvoiceResult.Successful();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate invoice {InvoiceId}", request.Irn);
            return ValidateInvoiceResult.Failure(ResponseMessages.INVOICE_VALIDATION_FAILED);
        }
    }

    private bool IsUserAuthorized() =>
    _currentUser.BusinessId.HasValue;
}
