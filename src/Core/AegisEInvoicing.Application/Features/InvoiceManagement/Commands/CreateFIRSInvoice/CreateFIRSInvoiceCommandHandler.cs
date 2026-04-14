using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Application.Services;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateFIRSInvoice;

public class CreateFIRSInvoiceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    ILogger<CreateFIRSInvoiceCommandHandler> logger,
    ITelemetryService? telemetryService = null) : IRequestHandler<CreateFIRSInvoiceCommand, CreateFIRSInvoiceResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ILogger<CreateFIRSInvoiceCommandHandler> _logger = logger;
    private readonly ITelemetryService? _telemetryService = telemetryService;

    public async Task<CreateFIRSInvoiceResult> Handle(CreateFIRSInvoiceCommand request, CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        Guid businessId = Guid.Empty; // Initialize to avoid unassigned variable error in catch block

        try
        {
            // SECURITY: Validate that the BusinessId in the request matches the authenticated user's BusinessId
            // This prevents a client from using their API key to create invoices for another business
            var authenticatedBusinessId = _currentUserService.BusinessId;

            if (authenticatedBusinessId.HasValue && authenticatedBusinessId.Value != Guid.Empty)
            {
                // User is authenticated (via API key), validate request.BusinessId matches
                if (request.BusinessId != Guid.Empty && request.BusinessId != authenticatedBusinessId.Value)
                {
                    _logger.LogWarning(
                        "Security violation: Attempted to create invoice for BusinessId {RequestedBusinessId} " +
                        "but authenticated with BusinessId {AuthenticatedBusinessId}",
                        request.BusinessId, authenticatedBusinessId.Value);

                    return new CreateFIRSInvoiceResult
                    {
                        Success = false,
                        Message = "Access denied. The BusinessId in your request does not match your authenticated business. " +
                                 "You can only create invoices for your own business."
                    };
                }

                // Use the authenticated business ID (ignore request.BusinessId even if it's Guid.Empty)
                businessId = authenticatedBusinessId.Value;
            }
            else if (request.BusinessId == Guid.Empty)
            {
                // No authenticated business and no business ID in request
                return new CreateFIRSInvoiceResult
                {
                    Success = false,
                    Message = "Invalid BusinessId. Please provide a valid Business ID or ensure your API key is associated with a business."
                };
            }
            else
            {
                // No authenticated business, use request.BusinessId
                businessId = request.BusinessId;
            }

            // Final validation - businessId should never be empty at this point
            if (businessId == Guid.Empty)
            {
                return new CreateFIRSInvoiceResult
                {
                    Success = false,
                    Message = "Invalid BusinessId. Please provide a valid Business ID or ensure your API key is associated with a business."
                };
            }

            // Validate that the business exists and has valid FIRS configuration
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

            if (business is null)
            {
                return new CreateFIRSInvoiceResult
                {
                    Success = false,
                    Message = $"Business not found with ID '{businessId}'. Please verify the Business ID is correct and the business exists in the system."
                };
            }

            if (string.IsNullOrWhiteSpace(business.ServiceId) || business.ServiceId.Length != 8)
            {
                return new CreateFIRSInvoiceResult
                {
                    Success = false,
                    Message = $"Business '{business.Name}' does not have a valid FIRS Service ID (8 characters required). Please complete FIRS registration first or contact support."
                };
            }

            // Get current user - either from traditional auth or find one from the business for API key auth
            User? currentUser = null;
            currentUser = await _context.Users
                .FirstOrDefaultAsync(u =>
                                         u.BusinessId == request.BusinessId &&
                                         !u.IsDeleted, cancellationToken);

            var createdById = currentUser?.Id ?? Guid.Empty;
            var isApiKeyAuth = _currentUserService.HasRole("ApiClient");


            // Step 1: Check if party already exists or create new one
            _logger.LogInformation("Checking for existing party with email {Email} for business {BusinessId}",
                request.Party.Email, businessId);

            // Look for existing party by email within the same business
            var existingParty = await _context.Parties
                .FirstOrDefaultAsync(p => p.TaxIdentificationNumber.Value == request.Party.TaxIdentificationNumber &&
                                         p.BusinessID == businessId &&
                                         !p.IsDeleted, cancellationToken);

            var partyExists = existingParty != null;

            existingParty = await _context.Parties
                .FirstOrDefaultAsync(p => p.Email == request.Party.Email &&
                                         p.BusinessID == businessId &&
                                         !p.IsDeleted, cancellationToken);

            partyExists = existingParty != null;

            Guid partyId;
            bool partyCreated = false;

            if (partyExists)
            {
                _logger.LogInformation("Found existing party {PartyId} with email {Email} for business {BusinessId}",
                    existingParty!.Id, request.Party.Email, businessId);

                partyId = existingParty.Id;

                // Optionally update party information if details have changed

                var hasChanges = false;

                if (existingParty.Name != request.Party.Name)
                {
                    existingParty.UpdateName(request.Party.Name);
                    hasChanges = true;
                }

                if (existingParty.Phone != request.Party.Phone || existingParty.Email != request.Party.Email)
                {
                    existingParty.UpdateContactInfo(request.Party.Phone, request.Party.Email);
                    hasChanges = true;
                }

                if (existingParty.TaxIdentificationNumber.Value != request.Party.TaxIdentificationNumber)
                {
                    var newTin = TIN.Create(request.Party.TaxIdentificationNumber);
                    existingParty.UpdateTaxIdentificationNumber(newTin);
                    hasChanges = true;
                }

                // Check if address has changed
                var currentAddress = existingParty.Address;
                var newAddress = Address.Create(
                    request.Party.Address.Street,
                    request.Party.Address.City,
                    request.Party.Address.State,
                    request.Party.Address.Country,
                    request.Party.Address.PostalCode ?? string.Empty);

                if (!AddressEquals(currentAddress, newAddress))
                {
                    existingParty.UpdateAddress(newAddress);
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation("Updated existing party {PartyId} with new details", existingParty.Id);
                }
            }
            else
            {
                _logger.LogInformation("No existing party found with email {Email}, creating new party for business {BusinessId}",
                    request.Party.Email, businessId);

                // Create value objects for the new party
                var tin = TIN.Create(request.Party.TaxIdentificationNumber);
                var address = Address.Create(
                    request.Party.Address.Street,
                    request.Party.Address.City,
                    request.Party.Address.State,
                    request.Party.Address.Country,
                    request.Party.Address.PostalCode ?? string.Empty);

                // Create Party entity directly
                var newParty = Party.Create(
                    request.Party.Name,
                    request.Party.Phone,
                    request.Party.Email,
                    tin,
                    address,
                    businessId,
                    request.Party.Description);

                // Save the party to database within the existing transaction
                await _context.Parties.AddAsync(newParty, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                partyId = newParty.Id;
                partyCreated = true;
                _logger.LogInformation("Successfully created new party {PartyId}", partyId);
            }

            // Step 2: Create Invoice directly
            _logger.LogInformation("Creating invoice for party {PartyId} in business {BusinessId}", partyId, businessId);

            // Generate the invoice reference number
            var irn = await GenerateInvoiceIRNAsync(business, request, cancellationToken);

            var invoiceNumber = await _context.Invoices
                .Where(i => i.Irn.Value == irn.Value && i.BusinessId == businessId)
                .FirstOrDefaultAsync(cancellationToken);

            if (invoiceNumber is not null)
                return new CreateFIRSInvoiceResult
                {
                    Success = false,
                    Message = $"Duplicate invoice detected. An invoice with IRN '{irn.Value}' already exists in the system. " +
                              $"Invoice was created on {invoiceNumber.CreatedAt:yyyy-MM-dd HH:mm:ss}. " +
                              "Please use a unique invoice number or verify if this is a duplicate submission."
                };

            // Create the invoice directly within the existing transaction
            var invoice = Invoice.Create(
                businessId,
                partyId,
                irn,
                business.InvoicePrefix,
                request.IssueDate,
                request.InvoiceType,
                request.Currency,
                request.DeliveryPeriod,
                request.PaymentMeans ?? PaymentMeans.Create("30", "Credit Transfer"),  // Default to credit transfer if not specified
                request.InvoiceSource,
                invoiceKind: request.InvoiceKind,
                note: request.Note,
                paymentReference: request.PaymentReference,
                paymentTerms: request.PaymentTerms,
                dueDate: request.DueDate,
                environmentMode: business.AppEnvironmentMode);

            if (string.IsNullOrEmpty(business.Certificate) ||
                string.IsNullOrEmpty(business.PublicKey))
                return new CreateFIRSInvoiceResult
                {
                    Success = false,
                    Message = $"QR Code generation failed for business '{business.Name}'. " +
                              "The business certificate or public key is not configured. " +
                              "Please contact your system administrator to complete the business setup."
                };

            var qrCode = InvoiceQrService.GenerateQrCode(
                               invoice.Irn,
                               business.Certificate!,
                               business.PublicKey!);

            invoice.SetQRCode(qrCode, []);
            invoice.CreatedBy = createdById;

            // Add invoice items - first we need to find or create BusinessItems
            var processedItems = new List<(Guid BusinessItemId, decimal Quantity, decimal UnitPrice, DiscountFeeDto? DiscountFee, AdditionalFeeDto? AdditionalFee)>();

            foreach (var itemDto in request.InvoiceItems)
            {
                Guid businessItemId;

                var serviceCode = ServiceCode.Create(itemDto.ServiceCode.Code, itemDto.ServiceCode.Name);

                // Find or create ItemCategory
                var itemCategory = await _context.ItemCategories
                    .FirstOrDefaultAsync(ic => ic.Name == itemDto.ItemCategory &&
                                             ic.BusinessID == businessId &&
                                             !ic.IsDeleted, cancellationToken);

                if (itemCategory == null)
                {
                    _logger.LogInformation("Creating new ItemCategory {CategoryName} for business {BusinessId}",
                        itemDto.ItemCategory, businessId);

                    // Create new ItemCategory
                    itemCategory = ItemCategory.Create(
                        itemDto.ItemCategory,
                        $"Auto-created category for {itemDto.ItemCategory}",
                        businessId);
                    await _context.ItemCategories.AddAsync(itemCategory, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Found existing ItemCategory {CategoryId} for {CategoryName}",
                        itemCategory.Id, itemDto.ItemCategory);
                }

                // Check if BusinessItem already exists with same name for this business
                // Query matches the unique constraint: (BusinessID, Name) where IsDeleted = false
                var existingBusinessItem = await _context.BusinessItems
                    .Include(bi => bi.ItemCategories)
                    .ThenInclude(ic => ic.ItemCategory)
                    .FirstOrDefaultAsync(bi => bi.Name == itemDto.Name &&
                                             bi.BusinessID == businessId &&
                                             !bi.IsDeleted, cancellationToken);

                if (existingBusinessItem != null)
                {
                    _logger.LogInformation("Found existing BusinessItem {ItemId} with name {ItemName} for business {BusinessId}",
                        existingBusinessItem.Id, itemDto.Name, businessId);

                    // Use existing BusinessItem
                    businessItemId = existingBusinessItem.Id;

                    // Add the category to the item if it doesn't already belong to it
                    if (!existingBusinessItem.BelongsToCategory(itemCategory.Id))
                    {
                        _logger.LogInformation("Adding category '{CategoryName}' to existing BusinessItem {ItemId}",
                            itemDto.ItemCategory, existingBusinessItem.Id);
                        existingBusinessItem.AddCategory(itemCategory.Id);
                    }

                    // Optionally update if price or other details have changed
                    var hasItemChanges = false;

                    // For FIRS/ERP integration, approval has already occurred externally
                    // Update price directly using UpdatePriceFromErp
                    if (existingBusinessItem.UnitPrice != itemDto.UnitPrice)
                    {
                        existingBusinessItem.UpdatePriceFromErp(itemDto.UnitPrice);
                        hasItemChanges = true;
                    }

                    if (existingBusinessItem.ItemDescription != itemDto.ItemDescription)
                    {
                        existingBusinessItem.UpdateDescription(itemDto.ItemDescription);
                        hasItemChanges = true;
                    }

                    // Update using the Update method which handles ServiceCode, Name, etc.
                    // Note: We keep the primary ItemCategoryId as-is, new categories are added to the collection
                    if (existingBusinessItem.ServiceCode.Code != serviceCode.Code ||
                        existingBusinessItem.ServiceCode.Name != serviceCode.Name ||
                        existingBusinessItem.Name != itemDto.Name)
                    {
                        existingBusinessItem.Update(
                            itemDto.Name,
                            ItemType.Service,
                            serviceCode,
                            existingBusinessItem.ItemCategoryId, // Keep existing primary category
                            itemDto.ItemDescription);
                        hasItemChanges = true;
                    }

                    if (hasItemChanges || !existingBusinessItem.BelongsToCategory(itemCategory.Id))
                    {
                        await _context.SaveChangesAsync(cancellationToken);
                        _logger.LogInformation("Updated existing BusinessItem {ItemId} with new details and/or categories", existingBusinessItem.Id);
                    }
                }
                else
                {
                    _logger.LogInformation("Creating new BusinessItem {ItemName} for business {BusinessId}",
                        itemDto.Name, businessId);

                    // Create new BusinessItem
                    var businessItem = BusinessItem.Create(
                        businessId,
                        itemDto.Name,
                        ItemType.Service,
                        serviceCode,
                        itemCategory.Id,
                        itemDto.ItemDescription,
                        itemDto.UnitPrice);

                    if (itemDto.TaxCategories.Count > 0)
                    {
                        var taxCategories = itemDto.TaxCategories.Select(tc =>
                            tc.IsPercentage
                                ? BusinessItemTaxCategory.CreatePercentage(tc.Name, tc.Name, tc.Percent!.Value)
                                : BusinessItemTaxCategory.CreateFlatFee(tc.Name, tc.Name, tc.FlatAmount!.Value)).ToList();
                        businessItem.UpdateTaxCategories(taxCategories);
                    }

                    await _context.BusinessItems.AddAsync(businessItem, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    businessItemId = businessItem.Id;
                    _logger.LogInformation("Successfully created new BusinessItem {ItemId}", businessItemId);
                }

                processedItems.Add((businessItemId, itemDto.Quantity, itemDto.UnitPrice, itemDto.DiscountFee, itemDto.AdditionalFee));
            }

            //  create invoice items with the  BusinessItemIds
            foreach (var (businessItemId, quantity, unitPrice, discountFee, additionalFee) in processedItems)
            {
                var invoiceItem = InvoiceItem.Create(
                    businessItemId,
                    invoice.Id,
                    quantity,
                    unitPrice, // Snapshot the price at invoice creation
                    discountFee == null ? null : DiscountFee.Create(discountFee.Amount, discountFee.Code),
                    additionalFee == null ? null : AdditionalFee.Create(additionalFee.Amount, additionalFee.Code));

                invoice.AddInvoiceItem(invoiceItem);
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
                        return new CreateFIRSInvoiceResult
                        {
                            Success = false,
                            Message = $"Invalid IRN format for billing reference: '{billingRefDto.Irn}'. " +
                                      "Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619). " +
                                      "Please check the IRN format and try again."
                        };
                    }

                    var billingRefIrn = IRN.CreateFromString(billingRefDto.Irn);
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
                    return new CreateFIRSInvoiceResult
                    {
                        Success = false,
                        Message = $"Invalid IRN format for dispatch document reference: '{request.DispatchDocumentReference.Irn}'. " +
                                  "Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)."
                    };
                }
                var dispatchRef = InvoiceDispatchDocumentReference.Create(
                    invoice.Id,
                    IRN.CreateFromString(request.DispatchDocumentReference.Irn),
                    request.DispatchDocumentReference.IssueDate);
                invoice.SetDispatchDocumentReference(dispatchRef);
            }

            if (request.ReceiptDocumentReference is not null)
            {
                if (!IRN.IsValidIRNFormat(request.ReceiptDocumentReference.Irn))
                {
                    return new CreateFIRSInvoiceResult
                    {
                        Success = false,
                        Message = $"Invalid IRN format for receipt document reference: '{request.ReceiptDocumentReference.Irn}'. " +
                                  "Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)."
                    };
                }
                var receiptRef = InvoiceReceiptDocumentReference.Create(
                    invoice.Id,
                    IRN.CreateFromString(request.ReceiptDocumentReference.Irn),
                    request.ReceiptDocumentReference.IssueDate);
                invoice.SetReceiptDocumentReference(receiptRef);
            }

            if (request.OriginatorDocumentReference is not null)
            {
                if (!IRN.IsValidIRNFormat(request.OriginatorDocumentReference.Irn))
                {
                    return new CreateFIRSInvoiceResult
                    {
                        Success = false,
                        Message = $"Invalid IRN format for originator document reference: '{request.OriginatorDocumentReference.Irn}'. " +
                                  "Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)."
                    };
                }
                var originatorRef = InvoiceOriginatorDocumentReference.Create(
                    invoice.Id,
                    IRN.CreateFromString(request.OriginatorDocumentReference.Irn),
                    request.OriginatorDocumentReference.IssueDate);
                invoice.SetOriginatorDocumentReference(originatorRef);
            }

            if (request.ContractDocumentReference is not null)
            {
                if (!IRN.IsValidIRNFormat(request.ContractDocumentReference.Irn))
                {
                    return new CreateFIRSInvoiceResult
                    {
                        Success = false,
                        Message = $"Invalid IRN format for contract document reference: '{request.ContractDocumentReference.Irn}'. " +
                                  "Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)."
                    };
                }
                var contractRef = InvoiceContractDocumentReference.Create(
                    invoice.Id,
                    IRN.CreateFromString(request.ContractDocumentReference.Irn),
                    request.ContractDocumentReference.IssueDate);
                invoice.SetContractDocumentReference(contractRef);
            }

            if (request.AdditionalDocumentReferences is not null && request.AdditionalDocumentReferences.Any())
            {
                foreach (var additionalRefDto in request.AdditionalDocumentReferences)
                {
                    if (!IRN.IsValidIRNFormat(additionalRefDto.Irn))
                    {
                        return new CreateFIRSInvoiceResult
                        {
                            Success = false,
                            Message = $"Invalid IRN format for additional document reference: '{additionalRefDto.Irn}'. " +
                                      "Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD (e.g., ITW00000001-E9E0C0D3-20240619)."
                        };
                    }
                    var additionalRef = InvoiceAdditionalDocumentReference.Create(
                        invoice.Id,
                        IRN.CreateFromString(additionalRefDto.Irn),
                        additionalRefDto.IssueDate);
                    invoice.AddAdditionalDocumentReference(additionalRef);
                }
            }

            // Explicitly mark the invoice as created by the current user


            await _context.Invoices.AddAsync(invoice, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Create invoice approval history
            var invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id,
                                                                      InvoiceStatus.CREATED,
                                                                      ResponseMessages.INVOICE_CREATED_SUCCESS);
            invoiceApprovalHistory.CreatedBy = createdById;

            await _context.InvoiceApprovalHistories.AddAsync(invoiceApprovalHistory);
            await _context.SaveChangesAsync(cancellationToken);

            //Take this block out when the flow engine is sorted
            invoiceApprovalHistory = InvoiceApprovalHistory.Create(invoice.Id,
                                                                       InvoiceStatus.APPROVED,
                                                                       ResponseMessages.INVOICE_APPROVED_SUCCESS);
            invoiceApprovalHistory.CreatedBy = createdById;

            await _context.InvoiceApprovalHistories.AddAsync(invoiceApprovalHistory);
            await _context.SaveChangesAsync(cancellationToken);

            var successMessage = partyCreated
                ? "Invoice created successfully with new party"
                : "Invoice created successfully using existing party";

            _logger.LogInformation("Successfully created invoice {InvoiceId} with party {PartyId} for business {BusinessId}. Party was {PartyStatus}",
                invoice.Id, partyId, businessId, partyCreated ? "created" : "reused");

            // Track successful invoice creation
            var duration = DateTime.UtcNow - startTime;
            _telemetryService?.TrackInvoiceCreated(invoice.Id, businessId, invoice.Irn.Value, duration);

            return new CreateFIRSInvoiceResult
            {
                Success = true,
                Message = successMessage,
                InvoiceId = invoice.Id,
                PartyId = partyId,
                IRN = invoice.Irn.Value
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice with party for business {BusinessId}", businessId);

            // Check for specific database exceptions and return user-friendly messages
            var errorMessage = ex switch
            {
                DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
                    => "Duplicate invoice detected. An invoice with this IRN or invoice number already exists in the system. Please use a unique invoice number.",

                DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("foreign key", StringComparison.OrdinalIgnoreCase) == true
                    => "Invalid reference data. One or more referenced entities (business, party, or item) do not exist. Please verify all IDs are correct.",

                DbUpdateException dbEx when dbEx.InnerException?.Message.Contains("constraint", StringComparison.OrdinalIgnoreCase) == true
                    => "Data constraint violation. The invoice data violates database constraints. Please check all required fields and value ranges.",

                InvalidOperationException => $"Invalid operation: {ex.Message}. Please verify your request data is correct.",

                ArgumentException => $"Invalid argument: {ex.Message}. Please check the invoice data and ensure all required fields are properly formatted.",

                _ => $"An unexpected error occurred while creating the invoice: {ex.Message}. Please contact support if this persists."
            };

            return new CreateFIRSInvoiceResult
            {
                Success = false,
                Message = errorMessage
            };
        }
    }

    /// <summary>
    /// Helper method to compare two Address objects for equality
    /// </summary>
    private static bool AddressEquals(Address address1, Address address2)
    {
        if (address1 == null && address2 == null) return true;
        if (address1 == null || address2 == null) return false;

        return address1.Street == address2.Street &&
               address1.City == address2.City &&
               address1.State == address2.State &&
               address1.Country == address2.Country &&
               address1.PostalCode == address2.PostalCode;
    }

    /// <summary>
    /// Generates the next invoice sequence number for the given business
    /// </summary>
    private async Task<InvoiceSequenceInfo> GenerateNextInvoiceNumberAsync(Guid businessId, CancellationToken cancellationToken)
    {
        // Get the last invoice for this business
        var lastInvoice = await _context.Invoices
            .Where(i => i.BusinessId == businessId)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (lastInvoice == null)
        {
            // First invoice for this business
            return new InvoiceSequenceInfo("INV", 001);
        }

        // Parse the last invoice information from the IRN
        var lastIrn = IRN.CreateFromString(lastInvoice.Irn.Value);
        var lastPrefix = lastIrn.GetPrefix();
        var lastSequenceNumber = lastIrn.GetSequenceNumber();

        // Increment the sequence number
        var nextSequenceNumber = lastSequenceNumber + 1;

        return new InvoiceSequenceInfo(lastPrefix, nextSequenceNumber);
    }


    private async Task<IRN> GenerateInvoiceIRNAsync(Business business, CreateFIRSInvoiceCommand request, CancellationToken cancellationToken)
    {
        var isNewInvoice = string.IsNullOrWhiteSpace(request.InvoiceNumber);

        if (isNewInvoice)
        {
            var seqInfo = await GenerateNextInvoiceNumberAsync(business.Id, cancellationToken);
            return IRN.Create(business.InvoicePrefix, business.ServiceId, seqInfo.SequenceNumber, request.IssueDate);
        }

        return IRN.Create(request.InvoiceNumber!, business.ServiceId, request.IssueDate);
    }
}
