using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;

public class CreateInvoiceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<CreateInvoiceCommandHandler> logger,
    IFlowRuleMatchingService flowRuleMatchingService,
    IConfiguration configuration) : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUserService;
    private readonly ILogger<CreateInvoiceCommandHandler> _logger = logger;
    private readonly IFlowRuleMatchingService _flowRuleMatchingService = flowRuleMatchingService;
    private readonly IConfiguration _configuration = configuration;

    public async Task<CreateInvoiceResult> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return (CreateInvoiceResult)CreateInvoiceResult.AuthorizationError();

            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == _currentUser.BusinessId, cancellationToken);

            if (business is null)
                return (CreateInvoiceResult)CreateInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            if (string.IsNullOrWhiteSpace(business.ServiceId) || business.ServiceId.Length != 8)
                return (CreateInvoiceResult)CreateInvoiceResult.BadRequest("Business does not have a valid FIRS Service ID. Please complete FIRS registration first.");

            var party = await _context.Parties
                .Where(p => p.Id == request.PartyId && p.BusinessID == _currentUser.BusinessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (party is null)
                return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(ResponseMessages.PARTY_NOT_FOUND);

            if (string.IsNullOrEmpty(business.Certificate) ||
                string.IsNullOrEmpty(business.PublicKey))
                return (CreateInvoiceResult)CreateInvoiceResult.NotFound(ResponseMessages.BUSINESS_QR_CODE_KEYS_NOT_CONFIGURED);

            var invoiceSequenceInfo = await GenerateNextInvoiceNumberAsync(business.Id, cancellationToken);

            var irn = IRN.Create(business.InvoicePrefix, business.ServiceId, invoiceSequenceInfo.SequenceNumber, request.IssueDate);

            var invoice = Invoice.Create(
                _currentUser.BusinessId!.Value,
                request.PartyId,
                irn,
                business.InvoicePrefix,
                request.IssueDate,
                request.InvoiceType,
                request.Currency,
                request.DeliveryPeriod,
                request.PaymentMeans,
                InvoiceSource.PORTAL,
                invoiceKind: request.InvoiceKind,
                note: request.Note,
                paymentReference: request.PaymentReference,
                paymentTerms: request.PaymentTerms,
                dueDate: request.DueDate,
                environmentMode: business.AppEnvironmentMode);

            var qrCode = InvoiceQrService.GenerateQrCode(
                                invoice.Irn,
                                business.Certificate!,
                                business.PublicKey!);

            invoice.SetQRCode(qrCode, []);

            decimal invoiceTotalAmount = 0;

            foreach (var itemDto in request.InvoiceItems)
            {
                var businessItem = await _context.BusinessItems
                    .Where(bi => bi.Id == itemDto.BusinessItemId && bi.BusinessID == _currentUser.BusinessId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (businessItem is null)
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(ResponseMessages.BUSINESS_ITEM_NOT_FOUND);

                var invoiceItem = InvoiceItem.Create(
                    itemDto.BusinessItemId,
                    invoice.Id,
                    itemDto.Quantity,
                    businessItem.UnitPrice, // Snapshot the current price
                    itemDto.DiscountFee == null ? null : DiscountFee.Create(itemDto.DiscountFee.Amount, itemDto.DiscountFee.Code),
                    itemDto.AdditionalFee == null ? null : AdditionalFee.Create(itemDto.AdditionalFee.Amount, itemDto.AdditionalFee.Code));

                invoice.AddInvoiceItem(invoiceItem);

                // Calculate total amount for this line item using the snapshot price
                var baseAmount = (decimal)(invoiceItem.UnitPriceSnapshot * itemDto.Quantity);
                var discount = (decimal)(itemDto.DiscountFee?.Amount ?? 0);
                var additional = (decimal)(itemDto.AdditionalFee?.Amount ?? 0);
                invoiceTotalAmount += baseAmount - discount + additional;
            }

            _logger.LogInformation("Calculated invoice total amount: {TotalAmount} for business {BusinessId}",
                                    invoiceTotalAmount, _currentUser.BusinessId);

            // Validate minimum invoice amount (prevent zero/negative invoices)
            var minInvoiceAmount = _configuration.GetValue<decimal>("InvoiceValidation:MinInvoiceAmount", 0.01m);

            if (invoiceTotalAmount < minInvoiceAmount)
            {
                _logger.LogWarning("Invoice amount {Amount} is below minimum threshold {MinAmount}",
                    invoiceTotalAmount, minInvoiceAmount);
                return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                    $"Invoice amount ({invoiceTotalAmount:N2}) is below minimum allowed amount ({minInvoiceAmount:N2}). " +
                    "Please verify the invoice items and quantities.");
            }

            // Note: Maximum invoice amount is NOT enforced here
            // FlowRule system handles approval routing based on invoice amounts
            // Large invoices will be routed to PENDING_APPROVAL if FlowRule requires it

            // Validate currency is still valid (redundant check, but critical for financial integrity)
            var supportedCurrencies = _configuration
                .GetSection("InvoiceValidation:SupportedCurrencies")
                .Get<string[]>() ?? new[] { "NGN" };

            if (!supportedCurrencies.Contains(request.Currency.Code, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Attempted to create invoice with unsupported currency: {Currency}",
                    request.Currency.Code);
                return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                    $"Currency '{request.Currency.Code}' is not supported. Supported currencies: {string.Join(", ", supportedCurrencies)}");
            }

            // Add billing references if provided
            if (request.BillingReferences is not null && request.BillingReferences.Any())
            {
                _logger.LogInformation("Adding {Count} billing references to invoice", request.BillingReferences.Count);

                foreach (var billingRefDto in request.BillingReferences)
                {
                    // Validate IRN format before creating value object
                    if (!IRN.IsValidIRNFormat(billingRefDto.Irn))
                    {
                        _logger.LogError("Invalid IRN format for billing reference: {Irn}. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD",
                            billingRefDto.Irn);
                        return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                            $"Invalid IRN format for billing reference: '{billingRefDto.Irn}'. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)");
                    }

                    var billingRefIrn = IRN.CreateFromString(billingRefDto.Irn);

                    var irnExists = await _context.Invoices
                                   .Where(i => i.BusinessId == _currentUser.BusinessId
                                    && i.Irn.Value == billingRefIrn.Value
                                    && i.IssueDate == billingRefDto.IssueDate)
                                   .AnyAsync(cancellationToken);

                    if (!irnExists)
                    {
                        return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                           $"Referenced IRN: '{billingRefDto.Irn}'. is not tied to any previously created invoice");
                    }

                    var billingReference = InvoiceBillingReference.Create(
                        invoice.Id,
                        billingRefIrn,
                        billingRefDto.IssueDate);

                    invoice.AddBillingReference(billingReference);
                    _logger.LogInformation("Added billing reference with IRN {Irn} and IssueDate {IssueDate}",
                        billingRefDto.Irn, billingRefDto.IssueDate);
                }
            }

            // Add document references if provided
            if (request.DispatchDocumentReference is not null)
            {
                if (!IRN.IsValidIRNFormat(request.DispatchDocumentReference.Irn))
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                        $"Invalid IRN format for dispatch document reference: '{request.DispatchDocumentReference.Irn}'");
                }

                var dispatchRefIrn = IRN.CreateFromString(request.DispatchDocumentReference.Irn);

                var irnExists = await _context.Invoices
                                 .Where(i => i.BusinessId == _currentUser.BusinessId
                                  && i.Irn.Value == dispatchRefIrn.Value
                                  && i.IssueDate == request.DispatchDocumentReference.IssueDate)
                                 .AnyAsync(cancellationToken);

                if (!irnExists)
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                       $"Referenced IRN: '{dispatchRefIrn}'. is not tied to any previously created invoice");
                }
                var dispatchRef = InvoiceDispatchDocumentReference.Create(
                    invoice.Id,
                    dispatchRefIrn,
                    request.DispatchDocumentReference.IssueDate);
                invoice.SetDispatchDocumentReference(dispatchRef);
            }

            if (request.ReceiptDocumentReference is not null)
            {
                if (!IRN.IsValidIRNFormat(request.ReceiptDocumentReference.Irn))
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                        $"Invalid IRN format for receipt document reference: '{request.ReceiptDocumentReference.Irn}'");
                }

                var receiptRefIrn = IRN.CreateFromString(request.ReceiptDocumentReference.Irn);

                var irnExists = await _context.Invoices
                                 .Where(i => i.BusinessId == _currentUser.BusinessId
                                  && i.Irn.Value == receiptRefIrn.Value
                                  && i.IssueDate == request.ReceiptDocumentReference.IssueDate)
                                 .AnyAsync(cancellationToken);

                if (!irnExists)
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                       $"Referenced IRN: '{receiptRefIrn}'. is not tied to any previously created invoice");
                }
                var receiptRef = InvoiceReceiptDocumentReference.Create(
                    invoice.Id,
                    receiptRefIrn,
                    request.ReceiptDocumentReference.IssueDate);
                invoice.SetReceiptDocumentReference(receiptRef);
            }

            if (request.OriginatorDocumentReference is not null)
            {
                if (!IRN.IsValidIRNFormat(request.OriginatorDocumentReference.Irn))
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                        $"Invalid IRN format for originator document reference: '{request.OriginatorDocumentReference.Irn}'");
                }

                var originatorRefIrn = IRN.CreateFromString(request.OriginatorDocumentReference.Irn);

                var irnExists = await _context.Invoices
                                 .Where(i => i.BusinessId == _currentUser.BusinessId
                                  && i.Irn.Value == originatorRefIrn.Value
                                  && i.IssueDate == request.OriginatorDocumentReference.IssueDate)
                                 .AnyAsync(cancellationToken);

                if (!irnExists)
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                       $"Referenced IRN: '{originatorRefIrn}'. is not tied to any previously created invoice");
                }

                var originatorRef = InvoiceOriginatorDocumentReference.Create(
                    invoice.Id,
                    originatorRefIrn,
                    request.OriginatorDocumentReference.IssueDate);
                invoice.SetOriginatorDocumentReference(originatorRef);
            }

            if (request.ContractDocumentReference is not null)
            {
                if (!IRN.IsValidIRNFormat(request.ContractDocumentReference.Irn))
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                        $"Invalid IRN format for contract document reference: '{request.ContractDocumentReference.Irn}'");
                }

                var contractRefIrn = IRN.CreateFromString(request.ContractDocumentReference.Irn);

                var irnExists = await _context.Invoices
                                 .Where(i => i.BusinessId == _currentUser.BusinessId
                                  && i.Irn.Value == contractRefIrn.Value
                                  && i.IssueDate == request.ContractDocumentReference.IssueDate)
                                 .AnyAsync(cancellationToken);

                if (!irnExists)
                {
                    return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                       $"Referenced IRN: '{contractRefIrn}'. is not tied to any previously created invoice");
                }

                var contractRef = InvoiceContractDocumentReference.Create(
                    invoice.Id,
                    contractRefIrn,
                    request.ContractDocumentReference.IssueDate);
                invoice.SetContractDocumentReference(contractRef);
            }

            if (request.AdditionalDocumentReferences is not null && request.AdditionalDocumentReferences.Any())
            {
                foreach (var additionalRefDto in request.AdditionalDocumentReferences)
                {
                    if (!IRN.IsValidIRNFormat(additionalRefDto.Irn))
                    {
                        return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                            $"Invalid IRN format for additional document reference: '{additionalRefDto.Irn}'");
                    }

                    var additionalRefIrn = IRN.CreateFromString(additionalRefDto.Irn);

                    var irnExists = await _context.Invoices
                                     .Where(i => i.BusinessId == _currentUser.BusinessId
                                      && i.Irn.Value == additionalRefIrn.Value
                                      && i.IssueDate == additionalRefDto.IssueDate)
                                     .AnyAsync(cancellationToken);

                    if (!irnExists)
                    {
                        return (CreateInvoiceResult)CreateInvoiceResult.BadRequest(
                           $"Referenced IRN: '{additionalRefIrn}'. is not tied to any previously created invoice");
                    }

                    var additionalRef = InvoiceAdditionalDocumentReference.Create(
                        invoice.Id,
                        additionalRefIrn,
                        additionalRefDto.IssueDate);
                    invoice.AddAdditionalDocumentReference(additionalRef);
                }
            }

            var matchingFlowRule = await _flowRuleMatchingService.FindMatchingFlowRuleAsync(
                                         _currentUser.BusinessId!.Value,
                                         invoiceTotalAmount,
                                         DateTimeOffset.UtcNow,
                                         cancellationToken);

            // Log FlowRule matching result for debugging
            if (matchingFlowRule != null)
            {
                _logger.LogInformation(
                    "FlowRule matched: '{FlowRuleName}' (ID: {FlowRuleId}) for amount {Amount}. " +
                    "MinAmount: {MinAmount}, MaxAmount: {MaxAmount}, RequiresApproval: {RequiresApproval}, Priority: {Priority}",
                    matchingFlowRule.Name,
                    matchingFlowRule.Id,
                    invoiceTotalAmount,
                    matchingFlowRule.MinAmount,
                    matchingFlowRule.MaxAmount,
                    matchingFlowRule.RequiresClientAdminApproval,
                    matchingFlowRule.Priority);
            }
            else
            {
                _logger.LogWarning(
                    "No FlowRule matched for business {BusinessId} with invoice amount {Amount}. " +
                    "Invoice will be auto-approved by default.",
                    _currentUser.BusinessId,
                    invoiceTotalAmount);
            }

            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync(cancellationToken);

            var invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id,
                                                                       InvoiceStatus.CREATED,
                                                                       ResponseMessages.INVOICE_CREATED_SUCCESS);

            await _context.InvoiceApprovalHistories.AddAsync(invoiceApprovalHistory);
            await _context.SaveChangesAsync(cancellationToken);

            string statusMessage;

            // If no flow rule exists, auto-approve the invoice
            if (matchingFlowRule is null)
            {
                _logger.LogInformation("No FlowRule configured for business {BusinessId}. Invoice will be auto-approved.",
                    _currentUser.BusinessId);

                invoice.UpdateStatus(InvoiceStatus.APPROVED);

                var autoApprovalHistory = InvoiceApprovalHistory.Create(
                    invoice.Id,
                    InvoiceStatus.APPROVED,
                    $"Invoice auto-approved (No approval rules configured, Amount: {invoiceTotalAmount:N2})");

                await _context.InvoiceApprovalHistories.AddAsync(autoApprovalHistory);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Invoice {InvoiceId} auto-approved - no FlowRules configured",
                    invoice.Id);

                statusMessage = "Invoice created and auto-approved";
            }
            else
            {
                _logger.LogInformation("Matched FlowRule '{FlowRuleName}' (ID: {FlowRuleId}) for invoice amount {Amount}. " +
                    "RequiresApproval: {RequiresApproval}",
                    matchingFlowRule.Name,
                    matchingFlowRule.Id,
                    invoiceTotalAmount,
                    matchingFlowRule.RequiresClientAdminApproval);

                // Apply FlowRule-based approval logic
                if (matchingFlowRule.RequiresClientAdminApproval && !_currentUser.Roles.Contains(RoleConstants.ClientAdmin))
                {
                    _logger.LogInformation(
                        "Invoice {InvoiceId} requires approval (User roles: {Roles}, Has ClientAdmin: {HasClientAdmin})",
                        invoice.Id,
                        string.Join(", ", _currentUser.Roles),
                        _currentUser.Roles.Contains(RoleConstants.ClientAdmin));

                    // Set invoice to pending approval
                    invoice.UpdateStatus(InvoiceStatus.PENDING_APPROVAL);

                    // Create pending approval history entry
                    var pendingApprovalHistory = InvoiceApprovalHistory.Create(
                        invoice.Id,
                        InvoiceStatus.PENDING_APPROVAL,
                        $"Invoice requires ClientAdmin approval (FlowRule: {matchingFlowRule.Name}, Amount: {invoiceTotalAmount:N2})");

                    await _context.InvoiceApprovalHistories.AddAsync(pendingApprovalHistory);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Invoice {InvoiceId} set to PENDING_APPROVAL, requires ClientAdmin approval",
                        invoice.Id);

                    statusMessage = "Invoice created and pending ClientAdmin approval";
                }
                else
                {
                    _logger.LogInformation(
                        "Invoice {InvoiceId} auto-approved. Reason: {Reason}",
                        invoice.Id,
                        matchingFlowRule.RequiresClientAdminApproval
                            ? "User has ClientAdmin role"
                            : "FlowRule does not require approval");

                    // Auto-approve the invoice
                    invoice.UpdateStatus(InvoiceStatus.APPROVED);

                    // Create auto-approval history entry
                    var autoApprovalHistory = InvoiceApprovalHistory.Create(
                        invoice.Id,
                        InvoiceStatus.APPROVED,
                        $"Invoice auto-approved (FlowRule: {matchingFlowRule.Name}, Amount: {invoiceTotalAmount:N2})");

                    await _context.InvoiceApprovalHistories.AddAsync(autoApprovalHistory);
                    await _context.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Invoice {InvoiceId} auto-approved based on FlowRule '{FlowRuleName}'",
                        invoice.Id, matchingFlowRule.Name);

                    statusMessage = "Invoice created and auto-approved";
                }
            }

            _logger.LogInformation("Invoice created successfully with ID: {InvoiceId}, Status: {Status}",
                invoice.Id, invoice.InvoiceStatus);

            return CreateInvoiceResult.Created(invoice.Id, $"{statusMessage} with IRN {invoice.Irn.Value}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return (CreateInvoiceResult)CreateInvoiceResult.Failure();
        }
    }

    private async Task<InvoiceSequenceInfo> GenerateNextInvoiceNumberAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var lastInvoice = await _context.Invoices
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastInvoice is null)
        {
            return new InvoiceSequenceInfo("INV", 001);
        }

        var lastIrn = IRN.CreateFromString(lastInvoice.Irn.Value);
        var lastPrefix = lastIrn.GetPrefix();
        var lastSequenceNumber = lastIrn.GetSequenceNumber();

        var nextSequenceNumber = lastSequenceNumber + 1;

        return new InvoiceSequenceInfo(lastPrefix, nextSequenceNumber);
    }


    private bool IsUserAuthorized() =>
     _currentUser.BusinessId.HasValue;
}
