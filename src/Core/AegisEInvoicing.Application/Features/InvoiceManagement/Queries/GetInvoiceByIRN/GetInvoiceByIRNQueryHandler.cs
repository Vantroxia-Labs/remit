using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceByIRN;

public class GetInvoiceByIRNQueryHandler : IRequestHandler<GetInvoiceByIRNQuery, GetInvoiceByIRNResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetInvoiceByIRNQueryHandler> _logger;

    public GetInvoiceByIRNQueryHandler(
        IApplicationDbContext context, 
        ICurrentUserService currentUserService,
        ILogger<GetInvoiceByIRNQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetInvoiceByIRNResult> Handle(GetInvoiceByIRNQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate IRN format
            if (string.IsNullOrWhiteSpace(request.IRN))
            {
                return new GetInvoiceByIRNResult
                {
                    Success = false,
                    Message = "IRN cannot be empty"
                };
            }

            // Validate user has business access
            if (request.BusinessId==Guid.Empty)
            {
                return new GetInvoiceByIRNResult
                {
                    Success = false,
                    Message = "User not authenticated or no business associated"
                };
            }

            _logger.LogInformation("Retrieving invoice with IRN: {IRN} for business: {BusinessId}", 
                request.IRN, request.BusinessId);

            // Build the query with business validation
            var query = _context.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceLine)
                    .ThenInclude(il => il.BusinessItem)
                        .ThenInclude(bi => bi.ItemCategory)
                .Include(i => i.Business)
                .Include(i => i.Party)
                .Include(i => i.CreatedByUser)
                .Include(i => i.InvoiceApprovalHistory)
                    .ThenInclude(ah => ah.CreatedByUser)
                .Include(i => i.BillingReferences)
                .Include(i => i.DispatchDocumentReference)
                .Include(i => i.ReceiptDocumentReference)
                .Include(i => i.OriginatorDocumentReference)
                .Include(i => i.ContractDocumentReference)
                .Include(i => i.AdditionalDocumentReferences)
                .Where(i => i.Irn.Value == request.IRN);

            // Apply business filtering for non-platform admins
            if (!_currentUserService.IsPlatformAdmin)
            {
                query = query.Where(i => i.BusinessId == request.BusinessId);
            }

            var invoice = await query.FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
            {
                _logger.LogWarning("Invoice with IRN: {IRN} not found for business: {BusinessId}", 
                    request.IRN, request.BusinessId);
                
                return new GetInvoiceByIRNResult
                {
                    Success = false,
                    Message = "Invoice not found"
                };
            }

            // Map to DTO
            var invoiceDto = new InvoiceDetailsDto
            {
                Id = invoice.Id,
                BusinessId = invoice.BusinessId,
                BusinessName = invoice.Business?.Name ?? "",
                Irn = invoice.Irn.Value,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                IssueTime = invoice.IssueTime,
                InvoiceType = invoice.InvoiceType,
                InvoiceSource = invoice.InvoiceSource,
                PaymentStatus = invoice.PaymentStatus,
                QrCodeImage = invoice.QRCode?.GetBase64String(),
                Note = invoice.Note,
                Currency = invoice.Currency,
                PaymentTerms = invoice.PaymentTerms,
                CurrentInvoiceStatus = invoice.InvoiceStatus,
                FirsResponseMessage = FirsResponse(invoice.FIRSSubmissionResponseMessage, invoice.InvoiceStatus),
                InvoiceStatus = [.. invoice.InvoiceApprovalHistory.Select(x => x.InvoiceStatus).OrderBy(x => x)],
                FIRSSubmissionId = invoice.FIRSSubmissionId,
                SubmittedToFIRSAt = invoice.SubmittedToFIRSAt,
                FIRSubmissionResponse = invoice.FIRSSubmissionResponseMessage,
                CreatedAt = invoice.CreatedAt,
                UpdatedAt = invoice.UpdatedAt,
                InvoiceItems = invoice.InvoiceLine.Select(item => new InvoiceItemDto
                {
                    Id = item.Id,
                    InvoiceId = item.InvoiceId,
                    ItemCode = item.BusinessItem.ItemId,
                    ServiceCode = item.BusinessItem.ServiceCode,
                    TaxCategory = item.BusinessItem.TaxCategory,
                    Category = item.BusinessItem.ItemCategory.Name,
                    ItemDescription = item.BusinessItem.ItemDescription,
                    DiscountFee = item.DiscountFee,
                    AdditionalFee = item.AdditionalFee,
                    UnitPrice = item.BusinessItem.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.Quantity * item.BusinessItem.UnitPrice
                }).ToList(),
                Party = new PartyDto
                {
                    Name = invoice.Party.Name,
                    Tin = invoice.Party.TaxIdentificationNumber,
                    Email = invoice.Party.Email,
                    Phone = invoice.Party.Phone,                    
                    Address = Address.Create(invoice.Party.Address.Street,
                                invoice.Party.Address.City,
                                invoice.Party.Address.State,
                                invoice.Party.Address.Country,
                                invoice.Party.Address.PostalCode ?? string.Empty)
                },
                PaymentMeans = invoice.PaymentMeans!,
                DeliveryPeriod = invoice.DeliveryPeriod!,
                InvoiceApprovalHistories = invoice.InvoiceApprovalHistory?.Select(ah => new InvoiceApprovalHistoryDto
                {
                    InvoiceStatus = ah.InvoiceStatus,
                    PerformedById = ah.CreatedBy,
                    PerformedBy = ah.CreatedByUser?.Email ?? "System",
                    Comments = ah.Comments
                }).ToList() ?? [],
                BillingReferences = invoice.BillingReferences?.Select(br => new BillingReferenceDto
                {
                    Id = br.Id,
                    Irn = br.Irn.Value,
                    IssueDate = br.IssueDate
                }).ToList(),
                DispatchDocumentReference = invoice.DispatchDocumentReference == null ? null : new DocumentReferenceDto
                {
                    Id = invoice.DispatchDocumentReference.Id,
                    Irn = invoice.DispatchDocumentReference.Irn.Value,
                    IssueDate = invoice.DispatchDocumentReference.IssueDate
                },
                ReceiptDocumentReference = invoice.ReceiptDocumentReference == null ? null : new DocumentReferenceDto
                {
                    Id = invoice.ReceiptDocumentReference.Id,
                    Irn = invoice.ReceiptDocumentReference.Irn.Value,
                    IssueDate = invoice.ReceiptDocumentReference.IssueDate
                },
                OriginatorDocumentReference = invoice.OriginatorDocumentReference == null ? null : new DocumentReferenceDto
                {
                    Id = invoice.OriginatorDocumentReference.Id,
                    Irn = invoice.OriginatorDocumentReference.Irn.Value,
                    IssueDate = invoice.OriginatorDocumentReference.IssueDate
                },
                ContractDocumentReference = invoice.ContractDocumentReference == null ? null : new DocumentReferenceDto
                {
                    Id = invoice.ContractDocumentReference.Id,
                    Irn = invoice.ContractDocumentReference.Irn.Value,
                    IssueDate = invoice.ContractDocumentReference.IssueDate
                },
                AdditionalDocumentReferences = invoice.AdditionalDocumentReferences?.Select(adr => new DocumentReferenceDto
                {
                    Id = adr.Id,
                    Irn = adr.Irn.Value,
                    IssueDate = adr.IssueDate
                }).ToList()
            };

            _logger.LogInformation("Successfully retrieved invoice with IRN: {IRN}", request.IRN);

            return new GetInvoiceByIRNResult
            {
                Success = true,
                Message = "Invoice retrieved successfully",
                Invoice = invoiceDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice with IRN: {IRN}", request.IRN);
            return new GetInvoiceByIRNResult
            {
                Success = false,
                Message = $"Error retrieving invoice: {ex.Message}"
            };
        }
    }

    private static string FirsResponse(string? message, InvoiceStatus invoiceStatus)
    {
        return invoiceStatus switch
        {
            InvoiceStatus.REJECTED => message ?? "Invoice Rejected",
            InvoiceStatus.VALIDATIONFAILED => message ?? "Invoice FIRS Validation Failed",
            InvoiceStatus.SIGNINGFAILED => message ?? "Invoice FIRS SIGNING Failed",
            InvoiceStatus.FAILED => message ?? "Invoice Transmission Failed",
            InvoiceStatus.CREATED => message ?? "Invoice Created",
            InvoiceStatus.APPROVED => message ?? "Invoice Approved",
            InvoiceStatus.VALIDATED => message ?? "Invoice FIRS Validation Successful",
            InvoiceStatus.SIGNED => message ?? "Invoice FIRS Signing Successful",
            _ => "Invoice Process Completed"
        };
    }
}