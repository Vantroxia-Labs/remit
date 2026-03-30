using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.DTOs;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceStatus;

public class GetInvoiceStatusQueryHandler : IRequestHandler<GetInvoiceStatusQuery, GetInvoiceStatusResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetInvoiceStatusQueryHandler> _logger;

    public GetInvoiceStatusQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetInvoiceStatusQueryHandler> logger)
    {
        _context = context;
        _currentUser = currentUserService;
        _logger = logger;
    }

    public async Task<GetInvoiceStatusResult> Handle(GetInvoiceStatusQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return (GetInvoiceStatusResult)GetInvoiceStatusResult.AuthorizationError();

            var businessId = request.BusinessId;

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null)
                return (GetInvoiceStatusResult)GetInvoiceStatusResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var invoice = await _context.Invoices
                   .AsNoTracking()
                   .Where(i => i.Id == request.InvoiceId
                            && i.BusinessId == businessId)
                   .Select(i => new InvoiceStatusDto
                   {
                       InvoiceId = i.Id,
                       BusinessId = i.BusinessId,
                       BusinessName = i.Business.Name,
                       IRN = i.Irn.Value,
                       IssueDate = i.IssueDate,
                       InvoiceStatus = i.InvoiceStatus,
                       PaymentStatus = i.PaymentStatus,
                       FIRSSubmissionId = i.FIRSSubmissionId,
                       SubmittedToFIRSAt = i.SubmittedToFIRSAt,
                       CreatedAt = i.CreatedAt,
                       UpdatedAt = i.UpdatedAt,
                       Note = i.Note,
                       CreatedBy = i.CreatedByUser.Email
                   })
                   .FirstOrDefaultAsync(cancellationToken);

            if (invoice is null)
                return (GetInvoiceStatusResult)GetInvoiceStatusResult.NotFound(ResponseMessages.INVOICE_NOT_FOUND);
                      
            _logger.LogInformation("Successfully retrieved status for invoice: {InvoiceId}",
                request.InvoiceId);

            return new GetInvoiceStatusResult
            {
                IsSuccess = true,
                Message = ResponseMessages.OPERATION_SUCCESSFUL,
                StatusCodes = HttpStatusCodes.OK.ToInt(),
                InvoiceStatus = invoice
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice status for ID: {InvoiceId}",
                request.InvoiceId);
            return (GetInvoiceStatusResult)GetInvoiceStatusResult.Failure(ResponseMessages.OPERATION_FAILED);
        }
    }

    private bool IsUserAuthorized() =>
    _currentUser.UserId.HasValue && _currentUser.BusinessId.HasValue;
}