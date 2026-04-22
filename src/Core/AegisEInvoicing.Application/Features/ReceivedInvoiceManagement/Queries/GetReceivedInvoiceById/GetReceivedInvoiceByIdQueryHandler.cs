using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoiceById;

/// <summary>
/// Handler for retrieving a single received invoice by ID with full details
/// </summary>
public sealed class GetReceivedInvoiceByIdQueryHandler : IRequestHandler<GetReceivedInvoiceByIdQuery, GetReceivedInvoiceByIdResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetReceivedInvoiceByIdQueryHandler> _logger;

    public GetReceivedInvoiceByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetReceivedInvoiceByIdQueryHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GetReceivedInvoiceByIdResult> Handle(GetReceivedInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Determine which business to authorize for
            var businessId = request.BusinessId ?? _currentUser.BusinessId;

            if (!businessId.HasValue)
            {
                _logger.LogWarning("No business ID provided and user has no associated business");
                return new GetReceivedInvoiceByIdResult
                {
                    Success = false,
                    Message = "Business ID is required"
                };
            }

            _logger.LogInformation(
                "Retrieving received invoice {InvoiceId} for business {BusinessId}",
                request.InvoiceId, businessId.Value);

            // Retrieve the invoice with full details
            var invoice = await _context.ReceivedInvoices
                .Include(ri => ri.Business)
                .Where(ri => !ri.IsDeleted
                    && ri.Id == request.InvoiceId
                    && ri.BusinessId == businessId.Value)
                .Select(ri => new ReceivedInvoiceDetailDto
                {
                    Id = ri.Id,
                    IRN = ri.Irn.Value,
                    InvoiceTypeCode = ri.InvoiceTypeCode,
                    IssueDate = ri.IssueDate,
                    IssueTime = ri.IssueTime,
                    DueDate = ri.DueDate,
                    DocumentCurrencyCode = ri.DocumentCurrencyCode,
                    TaxCurrencyCode = ri.TaxCurrencyCode,
                    PaymentStatus = ri.PaymentStatus,
                    EntryStatus = ri.EntryStatus,
                    SyncDate = ri.SyncDate,

                    // Supplier Information
                    SupplierPartyName = ri.SupplierPartyName,
                    SupplierTIN = ri.SupplierTIN.Value,
                    SupplierBRN = ri.SupplierBRN,
                    SupplierEmail = ri.SupplierEmail,
                    SupplierTelephone = ri.SupplierTelephone,
                    SupplierAddress = ri.SupplierAddress != null ? new AddressDto
                    {
                        StreetName = ri.SupplierAddress.Street,
                        CityName = ri.SupplierAddress.City,
                        CountrySubentity = ri.SupplierAddress.State,
                        CountryIdentificationCode = ri.SupplierAddress.Country,
                        PostalZone = ri.SupplierAddress.PostalCode,
                        Lga = ri.SupplierAddress.Lga,
                        AdditionalStreetName = null
                    } : null,

                    // Customer Information
                    CustomerPartyName = ri.CustomerPartyName,
                    CustomerTIN = ri.CustomerTIN.Value,
                    CustomerBRN = ri.CustomerBRN,
                    CustomerEmail = ri.CustomerEmail,
                    CustomerTelephone = ri.CustomerTelephone,
                    CustomerAddress = ri.CustomerAddress != null ? new AddressDto
                    {
                        StreetName = ri.CustomerAddress.Street,
                        CityName = ri.CustomerAddress.City,
                        CountrySubentity = ri.CustomerAddress.State,
                        CountryIdentificationCode = ri.CustomerAddress.Country,
                        PostalZone = ri.CustomerAddress.PostalCode,
                        Lga = ri.CustomerAddress.Lga,
                        AdditionalStreetName = null
                    } : null,

                    // Financial Amounts
                    LineExtensionAmount = ri.LineExtensionAmount,
                    TaxExclusiveAmount = ri.TaxExclusiveAmount,
                    TaxInclusiveAmount = ri.TaxInclusiveAmount,
                    TotalTaxAmount = ri.TotalTaxAmount,
                    PayableAmount = ri.PayableAmount,
                    PaidAmount = ri.PaidAmount,
                    PayableRoundingAmount = ri.PayableRoundingAmount,

                    // Additional Information
                    Note = ri.Note,
                    BuyerReference = ri.BuyerReference,
                    AccountingCost = ri.AccountingCost,

                    // Invoice Lines and Tax Totals (JSON)
                    InvoiceLinesJson = ri.InvoiceLinesJson,
                    TaxTotalJson = ri.TaxTotalJson,

                    // Business Association
                    BusinessId = ri.BusinessId,
                    BusinessName = ri.Business != null ? ri.Business.Name : null,

                    // Reconciliation Status
                    IsReconciled = ri.IsReconciled,
                    ReconciledAt = ri.ReconciledAt,
                    ReconciledBy = ri.ReconciledBy,

                    // Audit Fields
                    CreatedAt = ri.CreatedAt,
                    CreatedBy = ri.CreatedBy,
                    UpdatedAt = ri.UpdatedAt,
                    UpdatedBy = ri.UpdatedBy
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning(
                    "Received invoice {InvoiceId} not found for business {BusinessId}",
                    request.InvoiceId, businessId.Value);

                return new GetReceivedInvoiceByIdResult
                {
                    Success = false,
                    Message = "Invoice not found"
                };
            }

            _logger.LogInformation(
                "Successfully retrieved received invoice {InvoiceId}",
                request.InvoiceId);

            return new GetReceivedInvoiceByIdResult
            {
                Success = true,
                Message = "Invoice retrieved successfully",
                Invoice = invoice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving received invoice {InvoiceId}: {Message}",
                request.InvoiceId, ex.Message);

            return new GetReceivedInvoiceByIdResult
            {
                Success = false,
                Message = "An error occurred while retrieving the invoice"
            };
        }
    }
}
