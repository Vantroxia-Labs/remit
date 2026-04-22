using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models.InvoiceData;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.UploadInvoices;

public class UploadInvoiceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IInvoiceReferenceValidator invoiceReferenceValidator,
    IFlowRuleMatchingService flowRuleMatchingService,
    ILogger<UploadInvoiceCommandHandler> logger)
    : IRequestHandler<UploadInvoiceCommand, UploadInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IInvoiceReferenceValidator _invoiceReferenceValidator = invoiceReferenceValidator;
    private readonly IFlowRuleMatchingService _flowRuleMatchingService = flowRuleMatchingService;
    private readonly ILogger<UploadInvoiceCommandHandler> _logger = logger;

    public async Task<UploadInvoiceResult> Handle(UploadInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (request?.UploadInvoiceRequest == null || !request.UploadInvoiceRequest.Any())
            return (UploadInvoiceResult)UploadInvoiceResult.BadRequest(ResponseMessages.INVALID_INVOICE_DATA);

        var result = new BulkUploadInvoiceResult
        {
            TotalInvoices = request.UploadInvoiceRequest.Count
        };

        if (!IsUserAuthorized())
            return (UploadInvoiceResult)UploadInvoiceResult.AuthorizationError();

        var business = await _context.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == _currentUser.BusinessId, cancellationToken);

        if (business is null)
            return (UploadInvoiceResult)UploadInvoiceResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

        if (string.IsNullOrWhiteSpace(business.ServiceId) || business.ServiceId.Length != InvoiceConstants.FIRS_SERVICE_ID_LENGTH)
            return (UploadInvoiceResult)UploadInvoiceResult.BadRequest(ResponseMessages.BUSINESS_SERIVCE_CODE_MISSING);

        if (string.IsNullOrWhiteSpace(business.InvoicePrefix))
            return (UploadInvoiceResult)UploadInvoiceResult.BadRequest(ResponseMessages.BUSINESS_INVOICE_PREFIX_MISSING);

        if (string.IsNullOrEmpty(business.Certificate) ||
              string.IsNullOrEmpty(business.PublicKey))
            return (UploadInvoiceResult)UploadInvoiceResult.NotFound(ResponseMessages.BUSINESS_QR_CODE_KEYS_NOT_CONFIGURED);

        // Get starting sequence number
        var invoiceSequenceInfo = await GetLastInvoiceSequenceAsync(business.Id, cancellationToken);
        var currentSequenceNumber = invoiceSequenceInfo.SequenceNumber;

        // Pre-load and cache all parties, categories, and business items
        var cacheContext = await PreloadCacheDataAsync(request.UploadInvoiceRequest, cancellationToken);

        // Pre-validate all payment references in batch
        var paymentReferences = request.UploadInvoiceRequest
            .Select(i => i.PaymentReference)
            .Where(pr => !string.IsNullOrWhiteSpace(pr))
            .Distinct()
            .ToList();
        var existingPaymentRefsList = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == business.Id && i.PaymentReference != null && paymentReferences.Contains(i.PaymentReference))
            .Select(i => i.PaymentReference)
            .ToListAsync(cancellationToken);
        var existingPaymentRefs = new HashSet<string>(existingPaymentRefsList.Where(pr => !string.IsNullOrEmpty(pr))!);

        // Process invoices in batches
        var batchSize = InvoiceConstants.UPLOAD_BATCH_SIZE;
        var batches = request.UploadInvoiceRequest
            .Select((invoice, index) => new { invoice, index })
            .GroupBy(x => x.index / batchSize)
            .Select(g => g.Select(x => x.invoice).ToList())
            .ToList();

        foreach (var batch in batches)
        {
            await ProcessBatchAsync(
                batch,
                business,
                currentSequenceNumber,
                cacheContext,
                existingPaymentRefs,
                result,
                cancellationToken);
        }

        _logger.LogInformation(
            "Bulk upload complete: {Success} succeeded, {Failed} failed out of {Total}",
            result.SuccessCount, result.FailureCount, result.TotalInvoices);

        return new UploadInvoiceResult
        {
            IsSuccess = result.IsSuccess,
            TotalObjects = result.TotalInvoices,
            SuccessfulUploads = result.SuccessCount,
            FailedUploads = result.FailureCount,
            FailedUploadDetails = result.FailedInvoices.ToDictionary(f => f.Reference, f => f.Error),
            StatusCodes = HttpStatusCodes.OK.ToInt(),
            Message = "Bulk Data Processed"
        };
    }

    private async Task<int> ProcessBatchAsync(
        List<UploadInvoiceRequest> invoices,
        Business business,
        int currentSequenceNumber,
        CacheContext cacheContext,
        HashSet<string> existingPaymentRefs,
        BulkUploadInvoiceResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var invoicesToAdd = new List<Invoice>();
            var approvalHistoriesToAdd = new List<InvoiceApprovalHistory>();

            // Pre-validate all document references in this batch using the validator service
            var allDocumentReferences = new List<(string Irn, DateOnly IssueDate)>();
            foreach (var inv in invoices)
            {
                if (inv.BillingReference != null)
                    allDocumentReferences.AddRange(inv.BillingReference.Select(br => (br.Irn, br.IssueDate)));
                if (inv.DispatchDocumentReference != null)
                    allDocumentReferences.Add((inv.DispatchDocumentReference.Irn, inv.DispatchDocumentReference.IssueDate));
                if (inv.ReceiptDocumentReference != null)
                    allDocumentReferences.Add((inv.ReceiptDocumentReference.Irn, inv.ReceiptDocumentReference.IssueDate));
                if (inv.OriginatorDocumentReference != null)
                    allDocumentReferences.Add((inv.OriginatorDocumentReference.Irn, inv.OriginatorDocumentReference.IssueDate));
                if (inv.ContractDocumentReference != null)
                    allDocumentReferences.Add((inv.ContractDocumentReference.Irn, inv.ContractDocumentReference.IssueDate));
                if (inv.AdditionalDocumentReferences != null)
                    allDocumentReferences.AddRange(inv.AdditionalDocumentReferences.Select(adr => (adr.Irn, adr.IssueDate)));
            }

            // Single database query to validate all references
            var validatedReferences = await _invoiceReferenceValidator.ValidateMultipleIrnsAsync(
                allDocumentReferences,
                _currentUser.BusinessId!.Value,
                cancellationToken);

            foreach (var invoiceRequest in invoices)
            {
                try
                {
                    // Use pre-loaded HashSet for O(1) lookup instead of database query
                    if (existingPaymentRefs.Contains(invoiceRequest.PaymentReference))
                    {
                        result.AddFailure(
                        invoiceRequest.PaymentReference,
                        $"An invoice with the same payment reference, {invoiceRequest.PaymentReference}, already exists.");
                        continue;
                    }
                    // Get or create party
                    var partyId = await GetOrCreatePartyAsync(
                        invoiceRequest.Party!,
                        cacheContext,
                        cancellationToken);

                    // Process invoice items
                    var invoiceItems = await ProcessInvoiceItemsAsync(
                        invoiceRequest.InvoiceItems,
                        cacheContext,
                        cancellationToken);

                    // Generate IRN with incremented sequence
                    currentSequenceNumber++;
                    var irn = IRN.Create(
                        business.InvoicePrefix,
                        business.ServiceId,
                        currentSequenceNumber,
                        DateOnly.Parse(invoiceRequest.IssueDate));

                    // Create invoice
                    var invoice = CreateInvoiceWithItems(
                        invoiceRequest,
                        partyId,
                        irn,
                        business.InvoicePrefix,
                        invoiceItems,
                        business.AppEnvironmentMode);

                    var qrCode = InvoiceQrService.GenerateQrCode(
                               invoice.Irn,
                               business.Certificate!,
                               business.PublicKey!);

                    invoice.SetQRCode(qrCode, []);

                    // Add billing references if provided - using pre-validated results
                    if (invoiceRequest.BillingReference is not null && invoiceRequest.BillingReference.Any())
                    {
                        foreach (var billingRefDto in invoiceRequest.BillingReference.DistinctBy(inv => inv.Irn))
                        {
                            if (!IRN.IsValidIRNFormat(billingRefDto.Irn))
                            {
                                _logger.LogError("Invalid IRN format for billing reference: {Irn}", billingRefDto.Irn);
                                result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Invalid IRN format for billing reference: '{billingRefDto.Irn}'. Expected format: {InvoiceConstants.IRN_FORMAT_EXAMPLE}");
                                continue;
                            }

                            // Use pre-validated results (single database query for entire batch)
                            if (!validatedReferences.TryGetValue(billingRefDto.Irn, out var isValid) || !isValid)
                            {
                                _logger.LogError("Referenced IRN: '{Irn}' is not tied to any previously created invoice", billingRefDto.Irn);
                                result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Referenced IRN: '{billingRefDto.Irn}' is not tied to any previously created invoice");
                                continue;
                            }

                            var billingRefIrn = IRN.CreateFromString(billingRefDto.Irn);
                            var billingReference = InvoiceBillingReference.Create(
                                invoice.Id,
                                billingRefIrn,
                                billingRefDto.IssueDate);

                            invoice.AddBillingReference(billingReference);
                        }
                    }

                    // Add document references if provided
                    if (invoiceRequest.DispatchDocumentReference is not null)
                    {
                        if (!IRN.IsValidIRNFormat(invoiceRequest.DispatchDocumentReference.Irn))
                        {
                            _logger.LogError("Invalid IRN format for dispatch document reference: {Irn}. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD",
                                     invoiceRequest.DispatchDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Invalid IRN format for dispatch document reference: '{invoiceRequest.DispatchDocumentReference.Irn}'. Expected format: {InvoiceConstants.IRN_FORMAT_EXAMPLE}");
                            continue;
                        }

                        var dispatchRefIrn = IRN.CreateFromString(invoiceRequest.DispatchDocumentReference.Irn);

                        // Use pre-validated results (single database query for entire batch)
                        var irnExists = validatedReferences.TryGetValue(invoiceRequest.DispatchDocumentReference.Irn, out var isValidRef) && isValidRef;

                        if (!irnExists)
                        {
                            _logger.LogError($"Referenced IRN: '{invoiceRequest.DispatchDocumentReference.Irn}'. is not tied to any previously created invoice",
                                  invoiceRequest.DispatchDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Referenced IRN: '{invoiceRequest.DispatchDocumentReference.Irn}'. is not tied to any previously created invoice");
                            continue;
                        }
                        var dispatchRef = InvoiceDispatchDocumentReference.Create(
                            invoice.Id,
                            dispatchRefIrn,
                            invoiceRequest.DispatchDocumentReference.IssueDate);
                        invoice.SetDispatchDocumentReference(dispatchRef);
                    }

                    if (invoiceRequest.ReceiptDocumentReference is not null)
                    {
                        if (!IRN.IsValidIRNFormat(invoiceRequest.ReceiptDocumentReference.Irn))
                        {
                            _logger.LogError("Invalid IRN format for receipt document reference: {Irn}. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD",
                                     invoiceRequest.ReceiptDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Invalid IRN format for receipt document reference: '{invoiceRequest.ReceiptDocumentReference.Irn}'. Expected format: {InvoiceConstants.IRN_FORMAT_EXAMPLE}");
                            continue;
                        }

                        var receiptRefIrn = IRN.CreateFromString(invoiceRequest.ReceiptDocumentReference.Irn);

                        // Use pre-validated results (single database query for entire batch)
                        var irnExists = validatedReferences.TryGetValue(invoiceRequest.ReceiptDocumentReference.Irn, out var isValidRef) && isValidRef;

                        if (!irnExists)
                        {
                            _logger.LogError($"Referenced IRN: '{invoiceRequest.ReceiptDocumentReference.Irn}'. is not tied to any previously created invoice",
                                  invoiceRequest.ReceiptDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Referenced IRN: '{invoiceRequest.ReceiptDocumentReference.Irn}'. is not tied to any previously created invoice");
                            continue;
                        }
                        var receiptRef = InvoiceReceiptDocumentReference.Create(
                            invoice.Id,
                            receiptRefIrn,
                            invoiceRequest.ReceiptDocumentReference.IssueDate);
                        invoice.SetReceiptDocumentReference(receiptRef);
                    }

                    if (invoiceRequest.OriginatorDocumentReference is not null)
                    {
                        if (!IRN.IsValidIRNFormat(invoiceRequest.OriginatorDocumentReference.Irn))
                        {
                            _logger.LogError("Invalid IRN format for originator document reference: {Irn}. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD",
                                      invoiceRequest.OriginatorDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Invalid IRN format for originator document reference: '{invoiceRequest.OriginatorDocumentReference.Irn}'. Expected format: {InvoiceConstants.IRN_FORMAT_EXAMPLE}");
                            continue;
                        }

                        var originatorRefIrn = IRN.CreateFromString(invoiceRequest.OriginatorDocumentReference.Irn);

                        // Use pre-validated results (single database query for entire batch)
                        var irnExists = validatedReferences.TryGetValue(invoiceRequest.OriginatorDocumentReference.Irn, out var isValidRef) && isValidRef;

                        if (!irnExists)
                        {
                            _logger.LogError($"Referenced IRN: '{invoiceRequest.OriginatorDocumentReference.Irn}'. is not tied to any previously created invoice",
                                  invoiceRequest.OriginatorDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Referenced IRN: '{invoiceRequest.OriginatorDocumentReference.Irn}'. is not tied to any previously created invoice");
                            continue;
                        }

                        var originatorRef = InvoiceOriginatorDocumentReference.Create(
                            invoice.Id,
                            originatorRefIrn,
                            invoiceRequest.OriginatorDocumentReference.IssueDate);
                        invoice.SetOriginatorDocumentReference(originatorRef);
                    }

                    if (invoiceRequest.ContractDocumentReference is not null)
                    {
                        if (!IRN.IsValidIRNFormat(invoiceRequest.ContractDocumentReference.Irn))
                        {
                            _logger.LogError("Invalid IRN format for contract document reference: {Irn}. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD",
                                       invoiceRequest.ContractDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Invalid IRN format for contract document reference: '{invoiceRequest.ContractDocumentReference.Irn}'. Expected format: {InvoiceConstants.IRN_FORMAT_EXAMPLE}");
                            continue;
                        }

                        var contractRefIrn = IRN.CreateFromString(invoiceRequest.ContractDocumentReference.Irn);

                        // Use pre-validated results (single database query for entire batch)
                        var irnExists = validatedReferences.TryGetValue(invoiceRequest.ContractDocumentReference.Irn, out var isValidRef) && isValidRef;

                        if (!irnExists)
                        {
                            _logger.LogError($"Referenced IRN: '{invoiceRequest.ContractDocumentReference.Irn}'. is not tied to any previously created invoice",
                                  invoiceRequest.ContractDocumentReference.Irn);
                            result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Referenced IRN: '{invoiceRequest.ContractDocumentReference.Irn}'. is not tied to any previously created invoice");
                            continue;
                        }

                        var contractRef = InvoiceContractDocumentReference.Create(
                            invoice.Id,
                            contractRefIrn,
                            invoiceRequest.ContractDocumentReference.IssueDate);
                        invoice.SetContractDocumentReference(contractRef);
                    }

                    if (invoiceRequest.AdditionalDocumentReferences is not null && invoiceRequest.AdditionalDocumentReferences.Any())
                    {
                        foreach (var additionalRefDto in invoiceRequest.AdditionalDocumentReferences)
                        {
                            if (!IRN.IsValidIRNFormat(additionalRefDto.Irn))
                            {
                                _logger.LogError("Invalid IRN format for additional document reference: {Irn}", additionalRefDto.Irn);
                                result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Invalid IRN format for additional reference: '{additionalRefDto.Irn}'. Expected format: {InvoiceConstants.IRN_FORMAT_EXAMPLE}");
                                continue;
                            }

                            // Use pre-validated results (single database query for entire batch)
                            if (!validatedReferences.TryGetValue(additionalRefDto.Irn, out var isValid) || !isValid)
                            {
                                _logger.LogError("Referenced IRN: '{Irn}' is not tied to any previously created invoice", additionalRefDto.Irn);
                                result.AddFailure(invoiceRequest.PaymentReference,
                                    $"Referenced IRN: '{additionalRefDto.Irn}' is not tied to any previously created invoice");
                                continue;
                            }

                            var additionalRefIrn = IRN.CreateFromString(additionalRefDto.Irn);

                            var additionalRef = InvoiceAdditionalDocumentReference.Create(
                                invoice.Id,
                                additionalRefIrn,
                                additionalRefDto.IssueDate);
                            invoice.AddAdditionalDocumentReference(additionalRef);
                        }
                    }


                    // Calculate invoice total amount for FlowRule matching
                    decimal invoiceTotalAmount = 0;
                    foreach (var item in invoiceItems)
                    {
                        var baseAmount = (decimal)(item.UnitPrice * item.Quantity);
                        var discount = (decimal)(item.DiscountFee?.Amount ?? 0);
                        var additional = (decimal)(item.AdditionalFee?.Amount ?? 0);
                        invoiceTotalAmount += baseAmount - discount + additional;
                    }

                    // Find matching FlowRule for approval logic
                    var matchingFlowRule = await _flowRuleMatchingService.FindMatchingFlowRuleAsync(
                        _currentUser.BusinessId!.Value,
                        invoiceTotalAmount,
                        DateTimeOffset.UtcNow,
                        cancellationToken);

                    // Add CREATED approval history
                    approvalHistoriesToAdd.Add(InvoiceApprovalHistory.Create(
                        invoice.Id,
                        InvoiceStatus.CREATED,
                        ResponseMessages.INVOICE_CREATED_SUCCESS));

                    // Apply FlowRule-based approval logic
                    if (matchingFlowRule is null)
                    {
                        // No FlowRule configured - auto-approve
                        invoice.UpdateStatus(InvoiceStatus.APPROVED);
                        approvalHistoriesToAdd.Add(InvoiceApprovalHistory.Create(
                            invoice.Id,
                            InvoiceStatus.APPROVED,
                            $"Invoice auto-approved (No approval rules configured, Amount: {invoiceTotalAmount:N2})"));
                    }
                    else if (matchingFlowRule.RequiresClientAdminApproval)
                    {
                        // FlowRule requires approval - set to pending
                        invoice.UpdateStatus(InvoiceStatus.PENDING_APPROVAL);
                        approvalHistoriesToAdd.Add(InvoiceApprovalHistory.Create(
                            invoice.Id,
                            InvoiceStatus.PENDING_APPROVAL,
                            $"Invoice requires ClientAdmin approval (FlowRule: {matchingFlowRule.Name}, Amount: {invoiceTotalAmount:N2})"));
                    }
                    else
                    {
                        // FlowRule doesn't require approval - auto-approve
                        invoice.UpdateStatus(InvoiceStatus.APPROVED);
                        approvalHistoriesToAdd.Add(InvoiceApprovalHistory.Create(
                            invoice.Id,
                            InvoiceStatus.APPROVED,
                            $"Invoice auto-approved (FlowRule: {matchingFlowRule.Name}, Amount: {invoiceTotalAmount:N2})"));
                    }

                    invoicesToAdd.Add(invoice);
                    result.AddSuccess(invoice.Id, irn.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process invoice for party: {PartyName}",
                        invoiceRequest.Party?.Name ?? "Unknown");
                    result.AddFailure(
                        invoiceRequest.PaymentReference,
                        ex.Message);
                }
            }

            // Batch insert all entities
            if (invoicesToAdd.Any())
            {
                await _context.Invoices.AddRangeAsync(invoicesToAdd, cancellationToken);
                await _context.InvoiceApprovalHistories.AddRangeAsync(approvalHistoriesToAdd, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return currentSequenceNumber;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing batch of invoices");

            // Mark all invoices in this batch as failed
            foreach (var invoice in invoices)
            {
                result.AddFailure(
                    invoice.PaymentReference,
                    $"Batch processing failed: {ex.Message}");
            }

            return currentSequenceNumber;
        }
    }

    private async Task<CacheContext> PreloadCacheDataAsync(
        List<UploadInvoiceRequest> invoices,
        CancellationToken cancellationToken)
    {
        var cacheContext = new CacheContext();

        // Preload all parties by email
        var emails = invoices
            .Where(i => i.Party?.Email != null)
            .Select(i => i.Party.Email.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        var existingParties = await _context.Parties
            .Where(p => emails.Contains(p.Email.ToLower()) && p.BusinessID == _currentUser.BusinessId)
            .AsNoTracking()
            .Select(p => new { p.Email, p.Id })
            .ToListAsync(cancellationToken);

        foreach (var party in existingParties)
        {
            cacheContext.PartyCache[party.Email.ToLowerInvariant()] = party.Id;
        }

        return cacheContext;
    }

    private Invoice CreateInvoiceWithItems(
        UploadInvoiceRequest request,
        Guid partyId,
        IRN irn,
        string invoicePrefix,
        List<InvoiceItemData> invoiceItems,
        AppEnvironmentMode environmentMode = AppEnvironmentMode.Production)
    {
        // Parse InvoiceKind if provided
        Domain.Enums.InvoiceKind? invoiceKind = null;
        if (!string.IsNullOrWhiteSpace(request.InvoiceKind) &&
            Enum.TryParse<Domain.Enums.InvoiceKind>(request.InvoiceKind, true, out var parsedKind))
        {
            invoiceKind = parsedKind;
        }

        var invoice = Invoice.Create(
            _currentUser.BusinessId!.Value,
            partyId,
            irn,
            invoicePrefix,
            DateOnly.Parse(request.IssueDate),
            InvoiceType.Create(request.InvoiceType.Name, request.InvoiceType.Code),
            Currency.Create(request.Currency.Name, request.Currency.Code),
            DeliveryPeriod.Create(DateOnly.Parse(request.DeliveryPeriod.StartDate), DateOnly.Parse(request.DeliveryPeriod.EndDate)),
            PaymentMeans.Create(request.PaymentMeans.Code, request.PaymentMeans.Name),
            InvoiceSource.PORTAL,
            invoiceKind: invoiceKind,
            note: request.Note,
            paymentReference: request.PaymentReference,
            paymentTerms: request.PaymentTerms,
            dueDate: DateOnly.Parse(request.DueDate),
            environmentMode: environmentMode);

        foreach (var itemData in invoiceItems)
        {
            var invoiceItem = InvoiceItem.Create(
                itemData.BusinessItemId,
                invoice.Id,
                itemData.Quantity,
                itemData.UnitPrice, // Use the snapshot price
                itemData.DiscountFee,
                itemData.AdditionalFee);

            invoice.AddInvoiceItem(invoiceItem);
        }

        return invoice;
    }

    private async Task<InvoiceSequenceInfo> GetLastInvoiceSequenceAsync(
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var lastIrn = await _context.Invoices
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.Irn.Value)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(lastIrn))
            return new InvoiceSequenceInfo("INV", 0); // Start at 0, will increment to 1

        var irn = IRN.CreateFromString(lastIrn);
        return new InvoiceSequenceInfo(irn.GetPrefix(), irn.GetSequenceNumber());
    }

    private async Task<Guid> GetOrCreatePartyAsync(
        PartyRequest partyRequest,
        CacheContext cacheContext,
        CancellationToken cancellationToken)
    {
        if (partyRequest is null)
            throw new ArgumentNullException(nameof(partyRequest));

        if (string.IsNullOrWhiteSpace(partyRequest.Email))
            throw new ArgumentException("Party email is required");

        var normalizedEmail = partyRequest.Email.Trim().ToLowerInvariant();

        // Check cache first
        if (cacheContext.PartyCache.TryGetValue(normalizedEmail, out var cachedPartyId))
            return cachedPartyId;

        // Not in cache, check database
        var partyId = await _context.Parties
            .Where(p => p.Email.ToLower() == normalizedEmail && p.BusinessID == _currentUser.BusinessId)
            .Select(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (partyId != Guid.Empty)
        {
            cacheContext.PartyCache[normalizedEmail] = partyId;
            return partyId;
        }

        // Create new party
        if (partyRequest.Address is null)
            throw new ArgumentException("Party address is required");

        var party = Party.Create(
            partyRequest.Name,
            partyRequest.Phone,
            partyRequest.Email,
            TIN.Create(partyRequest.TaxIdentificationNumber),
            Address.Create(
                partyRequest.Address.Street,
                partyRequest.Address.City,
                partyRequest.Address.State,
                partyRequest.Address.Country,
                partyRequest.Address.PostalCode),
            _currentUser.BusinessId!.Value,
            partyRequest.Description);

        _context.Parties.Add(party);
        await _context.SaveChangesAsync(cancellationToken);

        cacheContext.PartyCache[normalizedEmail] = party.Id;
        return party.Id;
    }

    private async Task<List<InvoiceItemData>> ProcessInvoiceItemsAsync(
        List<InvoiceItemRequest> businessItems,
        CacheContext cacheContext,
        CancellationToken cancellationToken)
    {
        if (businessItems is null || businessItems.Count == 0)
            throw new ArgumentException("At least one invoice item is required");

        var result = new List<InvoiceItemData>();

        // Group items to reduce database queries
        var itemsToProcess = businessItems
            .Select(item => new
            {
                Item = item,
                ItemName = item.Name?.Trim() ?? throw new ArgumentException("Item name is required")
            })
            .ToList();

        // Build lookup keys for business items
        var lookupKeys = itemsToProcess
            .Select(x => new BusinessItemKey(
                x.ItemName,
                x.Item.UnitPrice))
            .Distinct()
            .ToList();

        // Load existing business items
        var itemNames = lookupKeys.Select(k => k.Name).Distinct().ToList();

        var existingBusinessItems = await _context.BusinessItems
            .Where(bi => itemNames.Contains(bi.Name) &&
                        bi.BusinessID == _currentUser.BusinessId)
            .AsNoTracking()
            .Select(bi => new { bi.Name, bi.UnitPrice, bi.Id })
            .ToListAsync(cancellationToken);

        var businessItemsDict = new Dictionary<BusinessItemKey, Guid>();
        foreach (var item in existingBusinessItems)
        {
            var key = new BusinessItemKey(item.Name, item.UnitPrice);
            businessItemsDict[key] = item.Id;
        }

        // Create missing business items
        var newBusinessItems = new List<BusinessItem>();

        foreach (var itemToProcess in itemsToProcess)
        {
            var item = itemToProcess.Item;
            var key = new BusinessItemKey(itemToProcess.ItemName, item.UnitPrice);

            if (!businessItemsDict.TryGetValue(key, out var businessItemId))
            {
                if (item.ServiceCode == null)
                    throw new ArgumentException($"Service code is required for item: {item.Name}");

                var newBusinessItem = BusinessItem.Create(
                    _currentUser.BusinessId!.Value,
                    item.Name,
                    ItemType.Service,
                    ServiceCode.Create(item.ServiceCode.Code, item.Name),
                    Guid.Empty,
                    item.ItemDescription,
                    item.UnitPrice);

                if (item.TaxCategories is { Count: > 0 })
                {
                    var taxCats = item.TaxCategories.Select(tc =>
                        tc.IsPercentage
                            ? BusinessItemTaxCategory.CreatePercentage(tc.Name, tc.Name, tc.Percent!.Value)
                            : BusinessItemTaxCategory.CreateFlatFee(tc.Name, tc.Name, tc.FlatAmount!.Value)).ToList();
                    newBusinessItem.UpdateTaxCategories(taxCats);
                }

                newBusinessItems.Add(newBusinessItem);
                businessItemsDict[key] = newBusinessItem.Id;
                businessItemId = newBusinessItem.Id;
            }

            if (item.Quantity <= 0)
                throw new ArgumentException($"Quantity must be greater than 0 for item: {item.Name}");

            result.Add(new InvoiceItemData(
                businessItemId,
                item.Quantity,
                item.UnitPrice, // Snapshot the price at invoice creation time
                item.DiscountFee != null ? DiscountFee.Create(item.DiscountFee.Amount, (FeeStandardUnit)Enum.Parse(typeof(FeeStandardUnit), item.DiscountFee.FeeStandardUnit)) : null,
                item.AdditionalFee != null ? AdditionalFee.Create(item.AdditionalFee.Amount, (FeeStandardUnit)Enum.Parse(typeof(FeeStandardUnit), item.AdditionalFee.FeeStandardUnit)) : null));
        }

        if (newBusinessItems.Count > 0)
        {
            _context.BusinessItems.AddRange(newBusinessItems);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    private bool IsUserAuthorized() =>
        _currentUser?.UserId.HasValue == true && _currentUser?.BusinessId.HasValue == true;

    private record InvoiceSequenceInfo(string Prefix, int SequenceNumber);
    private record InvoiceItemData(Guid BusinessItemId, decimal Quantity, decimal UnitPrice, DiscountFee? DiscountFee, AdditionalFee? AdditionalFee);

    private sealed record BusinessItemKey(string Name, decimal UnitPrice)
    {
        public bool Equals(BusinessItemKey? other)
        {
            if (other is null) return false;
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
                   UnitPrice == other.UnitPrice;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Name.ToLowerInvariant(),
                UnitPrice);
        }
    }

    private class CacheContext
    {
        public Dictionary<string, Guid> PartyCache { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private class BulkUploadInvoiceResult
    {
        public int TotalInvoices { get; set; }
        public int SuccessCount => SuccessfulInvoices.Count;
        public int FailureCount => FailedInvoices.Count;
        public List<InvoiceSuccess> SuccessfulInvoices { get; } = new();
        public List<InvoiceFailure> FailedInvoices { get; } = new();
        public bool IsSuccess => FailureCount == 0 && SuccessCount > 0;

        public void AddSuccess(Guid invoiceId, string irn)
        {
            SuccessfulInvoices.Add(new InvoiceSuccess(invoiceId, irn));
        }

        public void AddFailure(string reference, string error)
        {
            FailedInvoices.Add(new InvoiceFailure(reference, error));
        }

        public record InvoiceSuccess(Guid InvoiceId, string IRN);
        public record InvoiceFailure(string Reference, string Error);
    }
}