using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceById;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, GetInvoiceByIdResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetInvoiceByIdQueryHandler> _logger;

    public GetInvoiceByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetInvoiceByIdQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetInvoiceByIdResult> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
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
                .Where(i => i.Id == request.InvoiceId);

            if (!_currentUserService.IsPlatformAdmin && _currentUserService.BusinessId.HasValue)
            {
                query = query.Where(i => i.BusinessId == _currentUserService.BusinessId.Value);
            }

            var invoice = await query.FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
            {
                _logger.LogWarning("Invoice with ID {InvoiceId} not found", request.InvoiceId);
                return new GetInvoiceByIdResult
                {
                    Success = false,
                    Message = "Invoice not found"
                };
            }

            // Log any missing related entities for debugging
            if (invoice.Party is null)
                _logger.LogWarning("Invoice {InvoiceId} has null Party reference", invoice.Id);

            var itemsWithNullBusinessItem = invoice.InvoiceLine.Where(il => il.BusinessItem is null).Count();
            if (itemsWithNullBusinessItem > 0)
                _logger.LogWarning("Invoice {InvoiceId} has {Count} invoice items with null BusinessItem reference",
                    invoice.Id, itemsWithNullBusinessItem);

            var invoiceDto = new InvoiceDetailsDto
            {
                Id = invoice.Id,
                BusinessId = invoice.BusinessId,
                Irn = invoice.Irn.Value,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                IssueTime = invoice.IssueTime,
                InvoiceType = invoice.InvoiceType,
                InvoiceSource = invoice.InvoiceSource,
                PaymentStatus = invoice.PaymentStatus,
                QrCodeImage = invoice.QRCode?.GetBase64String(),
                PaymentMeans = invoice.PaymentMeans!,
                DeliveryPeriod = invoice.DeliveryPeriod!,
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
                InvoiceItems = [.. invoice.InvoiceLine.Select(item => new InvoiceItemDto
                {
                    Id = item.Id,
                    InvoiceId = item.InvoiceId,
                    ItemCode = item.BusinessItem?.ItemId ?? string.Empty,
                    ServiceCode = item.BusinessItem?.ServiceCode ?? ServiceCode.Create("UNKNOWN", "Unknown Service"),
                    Category = item.BusinessItem?.ItemCategory?.Name ?? "Unknown",
                    ItemDescription = item.BusinessItem?.ItemDescription ?? string.Empty,
                    DiscountFee = item.DiscountFee,
                    AdditionalFee = item.AdditionalFee,
                    UnitPrice = item.BusinessItem?.UnitPrice ?? 0.0m,
                    Quantity = item.Quantity,
                    TotalPrice = item.Quantity * (item.BusinessItem?.UnitPrice ?? 0.0m)
                })],
                Party = invoice.Party != null ? new PartyDto
                {
                    Name = invoice.Party.Name ?? string.Empty,
                    Tin = invoice.Party.TaxIdentificationNumber,
                    Email = invoice.Party.Email ?? string.Empty,
                    Phone = invoice.Party.Phone ?? string.Empty,
                    Address = invoice.Party.Address != null ? Address.Create(
                                invoice.Party.Address.Street ?? string.Empty,
                                invoice.Party.Address.City ?? string.Empty,
                                invoice.Party.Address.State ?? string.Empty,
                                invoice.Party.Address.Country ?? string.Empty,
                                invoice.Party.Address.PostalCode ?? string.Empty)
                            : Address.Create(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
                } : new PartyDto
                {
                    Name = "Unknown Party",
                    Email = string.Empty,
                    Phone = string.Empty,
                    Address = Address.Create(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
                },
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

            return new GetInvoiceByIdResult
            {
                Success = true,
                Message = "Invoice retrieved successfully",
                Invoice = invoiceDto
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice with ID: {InvoiceId}", request.InvoiceId);
            return new GetInvoiceByIdResult
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