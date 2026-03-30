using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceIrns;

public class GetInvoiceIrnsQueryHandler : IRequestHandler<GetInvoiceIrnsQuery, GetInvoiceIrnsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetInvoiceIrnsQueryHandler> _logger;

    public GetInvoiceIrnsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<GetInvoiceIrnsQueryHandler> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetInvoiceIrnsResult> Handle(GetInvoiceIrnsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving invoice IRNs for business");

            if (!IsUserAuthorized())
                return (GetInvoiceIrnsResult)GetInvoiceIrnsResult.AuthorizationError();

            var business = await _context.Businesses
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == _currentUserService.BusinessId, cancellationToken);

            if (business is null)
                return (GetInvoiceIrnsResult)GetInvoiceIrnsResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            var irns = await _context.Invoices
                                .AsNoTracking()
                                .Where(i => i.BusinessId == _currentUserService.BusinessId)
                                .OrderByDescending(i => i.CreatedAt)
                                .Select(i => new InvoiceIrnData
                                {
                                    Irn = i.Irn.Value,
                                    IssueDate = i.IssueDate
                                })
                                .ToListAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} invoice IRNs", irns.Count);

           return GetInvoiceIrnsResult.Successful(irns);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice IRNs");
            return (GetInvoiceIrnsResult)GetInvoiceIrnsResult.Successful();
        }
    }


    private bool IsUserAuthorized() =>
    _currentUserService.BusinessId.HasValue;
}
