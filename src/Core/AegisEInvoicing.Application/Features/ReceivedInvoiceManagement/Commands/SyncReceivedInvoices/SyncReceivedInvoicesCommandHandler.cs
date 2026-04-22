using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Commands.SyncReceivedInvoices;

/// <summary>
/// Handler for synchronizing received invoices from APP provider
/// Creates new invoices or updates existing ones with the latest data from the provider
/// </summary>
public sealed class SyncReceivedInvoicesCommandHandler : IRequestHandler<SyncReceivedInvoicesCommand, SyncReceivedInvoicesResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IAppProviderRouter _appProviderRouter;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SyncReceivedInvoicesCommandHandler> _logger;

    public SyncReceivedInvoicesCommandHandler(
        IApplicationDbContext context,
        IAppProviderRouter appProviderRouter,
        ICurrentUserService currentUser,
        ILogger<SyncReceivedInvoicesCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _appProviderRouter = appProviderRouter ?? throw new ArgumentNullException(nameof(appProviderRouter));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SyncReceivedInvoicesResult> Handle(SyncReceivedInvoicesCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = _currentUser.UserId ?? Guid.Parse("9c17ea5c-483c-44f8-97e8-c364e6739949");

            _logger.LogInformation(
                "Starting sync of received invoices for business {BusinessId} from {StartDate} to {EndDate}",
                request.BusinessId, request.StartDate, request.EndDate);

            // Get the business and validate it exists
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId && !b.IsDeleted, cancellationToken);

            if (business == null)
            {
                _logger.LogWarning("Business {BusinessId} not found", request.BusinessId);
                return new SyncReceivedInvoicesResult
                {
                    Success = false,
                    Message = "Business not found",
                    Errors = ["Business not found or has been deleted"]
                };
            }

            // Validate the business has a TIN
            if (business.TaxIdentificationNumber == null || string.IsNullOrWhiteSpace(business.TaxIdentificationNumber.Value))
            {
                _logger.LogWarning("Business {BusinessId} does not have a valid TIN", request.BusinessId);
                return new SyncReceivedInvoicesResult
                {
                    Success = false,
                    Message = "Business does not have a valid TIN",
                    Errors = new List<string> { "Business must have a valid Tax Identification Number (TIN) to sync invoices" }
                };
            }

            // Get configured provider adapter for this business
            var provider = await _appProviderRouter.GetProviderAsync(request.BusinessId, cancellationToken);

            // Call APP provider API to get purchase invoices
            _logger.LogInformation(
                "Calling {Provider} API for purchase invoices with TIN {TIN}",
                provider.DisplayName, business.TaxIdentificationNumber.Value);

            var result = await provider.GetPurchaseInvoicesAsync(
                business.TaxIdentificationNumber.Value,
                DateOnly.Parse(request.StartDate),
                DateOnly.Parse(request.EndDate),
                cancellationToken);

            if (!result.IsSuccess || result.Count < 1 || result.Items == null)
            {
                return new SyncReceivedInvoicesResult
                {
                    Success = true,
                    Message = "No Recieved Invoices",
                    Errors = []
                };
            }

            _logger.LogInformation(
                "Found {Count} invoices from {Provider} for business {BusinessId}",
                result.Count, provider.DisplayName, request.BusinessId);

            // Get existing invoices to check for updates
            var invoiceIRNs = result.Items.Select(i => i.IRN).ToList();
            var existingInvoices = await _context.ReceivedInvoices
                .Where(ri => invoiceIRNs.Contains(ri.Irn.Value) && !ri.IsDeleted && ri.BusinessId == request.BusinessId)
                .ToListAsync(cancellationToken);

            var existingInvoiceDict = existingInvoices.ToDictionary(i => i.Irn.Value);

            int created = 0;
            int updated = 0;
            var errors = new List<string>();

            // Process each invoice
            foreach (var item in result.Items)
            {
                try
                {
                    // Check if invoice already exists
                    if (existingInvoiceDict.TryGetValue(item.IRN, out var existingInvoice))
                    {
                        // Update existing invoice with latest data
                        var supplierAddress = item.SupplierParty?.Address != null
                            ? Domain.ValueObjects.Address.Create(
                                street: CombineStreetNames(item.SupplierParty.Address.StreetName, item.SupplierParty.Address.AdditionalStreetName),
                                city: item.SupplierParty.Address.CityName ?? "Unknown",
                                state: item.SupplierParty.Address.CountrySubentity ?? "Unknown",
                                country: item.SupplierParty.Address.CountryIdentificationCode ?? "NG",
                                postalCode: item.SupplierParty.Address.PostalZone ?? string.Empty)
                            : null;

                        var customerAddress = item.CustomerParty?.Address != null
                            ? Domain.ValueObjects.Address.Create(
                                street: CombineStreetNames(item.CustomerParty.Address.StreetName, item.CustomerParty.Address.AdditionalStreetName),
                                city: item.CustomerParty.Address.CityName ?? "Unknown",
                                state: item.CustomerParty.Address.CountrySubentity ?? "Unknown",
                                country: item.CustomerParty.Address.CountryIdentificationCode ?? "NG",
                                postalCode: item.CustomerParty.Address.PostalZone ?? string.Empty)
                            : null;

                        existingInvoice.UpdateAllFromSync(
                            invoiceTypeCode: item.InvoiceTypeCode,
                            issueDate: DateOnly.FromDateTime(item.IssueDate),
                            documentCurrencyCode: item.DocumentCurrencyCode,
                            taxCurrencyCode: item.TaxCurrencyCode,
                            paymentStatus: item.PaymentStatus,
                            entryStatus: item.EntryStatus,
                            supplierPartyName: item.SupplierParty?.PartyName ?? "Unknown Supplier",
                            supplierTIN: Domain.ValueObjects.TIN.Create(item.SupplierParty?.TIN ?? "0000000000"),
                            customerPartyName: item.CustomerParty?.PartyName ?? "Unknown Customer",
                            customerTIN: Domain.ValueObjects.TIN.Create(item.CustomerParty?.TIN ?? business.TaxIdentificationNumber.Value),
                            lineExtensionAmount: item.LegalMonetaryTotal!.LineExtensionAmount,
                            taxExclusiveAmount: item.LegalMonetaryTotal!.TaxExclusiveAmount,
                            taxInclusiveAmount: item.LegalMonetaryTotal!.TaxInclusiveAmount,
                            totalTaxAmount: item.LegalMonetaryTotal!.TaxInclusiveAmount - item.LegalMonetaryTotal!.TaxExclusiveAmount,
                            payableAmount: item.LegalMonetaryTotal!.PayableAmount,
                            updatedBy: userId,
                            issueTime: item.IssueTime,
                            dueDate: item.DueDate.HasValue ? DateOnly.FromDateTime(item.DueDate.Value) : null,
                            syncDate: item.SyncDate,
                            supplierBRN: item.SupplierParty?.BRN,
                            supplierEmail: item.SupplierParty?.Email,
                            supplierTelephone: item.SupplierParty?.Telephone,
                            supplierAddress: supplierAddress,
                            customerBRN: item.CustomerParty?.BRN,
                            customerEmail: item.CustomerParty?.Email,
                            customerTelephone: item.CustomerParty?.Telephone,
                            customerAddress: customerAddress,
                            paidAmount: item.LegalMonetaryTotal?.PaidAmount,
                            payableRoundingAmount: item.LegalMonetaryTotal?.PayableRoundingAmount,
                            note: item.Note,
                            buyerReference: item.BuyerReference,
                            accountingCost: item.AccountingCost,
                            invoiceLinesJson: item.InvoiceLines != null && item.InvoiceLines.Any()
                                ? JsonSerializer.Serialize(item.InvoiceLines)
                                : null,
                            taxTotalJson: item.TaxTotal != null && item.TaxTotal.Any()
                                ? JsonSerializer.Serialize(item.TaxTotal)
                                : null);

                        updated++;
                        _logger.LogDebug("Updated existing invoice {IRN} for business {BusinessId}",
                            item.IRN, request.BusinessId);
                    }
                    else
                    {
                        // Create new invoice
                        // Map Interswitch address fields to domain Address (Street, City, State, Country, PostalCode)
                        var supplierAddress = item.SupplierParty?.Address != null
                            ? Domain.ValueObjects.Address.Create(
                                street: CombineStreetNames(item.SupplierParty.Address.StreetName, item.SupplierParty.Address.AdditionalStreetName),
                                city: item.SupplierParty.Address.CityName ?? "Unknown",
                                state: item.SupplierParty.Address.CountrySubentity ?? "Unknown",
                                country: item.SupplierParty.Address.CountryIdentificationCode ?? "NG",
                                postalCode: item.SupplierParty.Address.PostalZone ?? string.Empty)
                            : null;

                        var customerAddress = item.CustomerParty?.Address != null
                            ? Domain.ValueObjects.Address.Create(
                                street: CombineStreetNames(item.CustomerParty.Address.StreetName, item.CustomerParty.Address.AdditionalStreetName),
                                city: item.CustomerParty.Address.CityName ?? "Unknown",
                                state: item.CustomerParty.Address.CountrySubentity ?? "Unknown",
                                country: item.CustomerParty.Address.CountryIdentificationCode ?? "NG",
                                postalCode: item.CustomerParty.Address.PostalZone ?? string.Empty)
                            : null;

                        var receivedInvoice = ReceivedInvoice.Create(
                            businessId: business.Id,
                            firsBusinessId: item.BusinessId!,
                            irn: Domain.ValueObjects.IRN.CreateFromString(item.IRN),
                            invoiceTypeCode: item.InvoiceTypeCode,
                            issueDate: DateOnly.FromDateTime(item.IssueDate),
                            documentCurrencyCode: item.DocumentCurrencyCode,
                            taxCurrencyCode: item.TaxCurrencyCode,
                            paymentStatus: item.PaymentStatus,
                            entryStatus: item.EntryStatus,
                            supplierPartyName: item.SupplierParty?.PartyName ?? "Unknown Supplier",
                            supplierTIN: Domain.ValueObjects.TIN.Create(item.SupplierParty?.TIN ?? "0000000000"),
                            customerPartyName: item.CustomerParty?.PartyName ?? "Unknown Customer",
                            customerTIN: Domain.ValueObjects.TIN.Create(item.CustomerParty?.TIN ?? business.TaxIdentificationNumber.Value),
                            lineExtensionAmount: item.LegalMonetaryTotal!.LineExtensionAmount,
                            taxExclusiveAmount: item.LegalMonetaryTotal!.TaxExclusiveAmount,
                            taxInclusiveAmount: item.LegalMonetaryTotal!.TaxInclusiveAmount,
                            totalTaxAmount: item.LegalMonetaryTotal!.TaxInclusiveAmount - item.LegalMonetaryTotal!.TaxExclusiveAmount,
                            payableAmount: item.LegalMonetaryTotal!.PayableAmount,
                            createdBy: userId,
                            issueTime: item.IssueTime,
                            dueDate: item.DueDate.HasValue ? DateOnly.FromDateTime(item.DueDate.Value) : null,
                            syncDate: item.SyncDate,
                            supplierBRN: item.SupplierParty?.BRN,
                            supplierEmail: item.SupplierParty?.Email,
                            supplierTelephone: item.SupplierParty?.Telephone,
                            supplierAddress: supplierAddress,
                            customerBRN: item.CustomerParty?.BRN,
                            customerEmail: item.CustomerParty?.Email,
                            customerTelephone: item.CustomerParty?.Telephone,
                            customerAddress: customerAddress,
                            paidAmount: item.LegalMonetaryTotal?.PaidAmount,
                            payableRoundingAmount: item.LegalMonetaryTotal?.PayableRoundingAmount,
                            note: item.Note,
                            buyerReference: item.BuyerReference,
                            accountingCost: item.AccountingCost,
                            invoiceLinesJson: item.InvoiceLines != null && item.InvoiceLines.Any()
                                ? JsonSerializer.Serialize(item.InvoiceLines)
                                : null,
                            taxTotalJson: item.TaxTotal != null && item.TaxTotal.Any()
                                ? JsonSerializer.Serialize(item.TaxTotal)
                                : null);

                        await _context.ReceivedInvoices.AddAsync(receivedInvoice, cancellationToken);
                        created++;

                        _logger.LogDebug("Created new invoice {IRN}", item.IRN);
                    }

                    var confirmResult = await provider.ConfirmInvoiceAsync(item.IRN, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing invoice {IRN} for business {BusinessId}: {Message}",
                        item.IRN, request.BusinessId, ex.Message);

                    errors.Add($"Error processing invoice {item.IRN}: {ex.Message}");
                }
            }

            // Save all changes
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Completed sync for business {BusinessId}. Created: {Created}, Updated: {Updated}, Errors: {ErrorCount}",
                request.BusinessId, created, updated, errors.Count);

            return new SyncReceivedInvoicesResult
            {
                Success = true,
                Message = $"Successfully synced invoices. {created} new invoices created, {updated} existing invoices updated",
                InvoicesSynced = created + updated,
                InvoicesCreated = created,
                InvoicesSkipped = 0,
                InvoicesUpdated = updated,
                Errors = errors
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error syncing invoices for business {BusinessId}: {Message}",
                request.BusinessId, ex.Message);

            return new SyncReceivedInvoicesResult
            {
                Success = false,
                Message = "An error occurred while syncing invoices",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Combines street name and additional street name into a single street field
    /// </summary>
    private static string CombineStreetNames(string? streetName, string? additionalStreetName)
    {
        if (string.IsNullOrWhiteSpace(streetName) && string.IsNullOrWhiteSpace(additionalStreetName))
            return "Unknown";

        if (string.IsNullOrWhiteSpace(additionalStreetName))
            return streetName!.Trim();

        if (string.IsNullOrWhiteSpace(streetName))
            return additionalStreetName!.Trim();

        return $"{streetName.Trim()}, {additionalStreetName.Trim()}";
    }
}
