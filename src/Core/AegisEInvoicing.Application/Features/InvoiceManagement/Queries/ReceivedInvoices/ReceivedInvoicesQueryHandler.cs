using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Extensions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.ReceivedInvoices;

public class ReceivedInvoicesQueryHandler(IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<ReceivedInvoicesQueryHandler> logger)
    : IRequestHandler<ReceivedInvoicesQuery, PaginatedList<ReceivedInvoicesDto>>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<ReceivedInvoicesQueryHandler> _logger = logger;
    public async Task<PaginatedList<ReceivedInvoicesDto>> Handle(ReceivedInvoicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!IsUserAuthorized())
                return (PaginatedList<ReceivedInvoicesDto>)PaginatedList<ReceivedInvoicesDto>.AuthorizationError();

            //var businessId = _currentUser.BusinessId!.Value;

            //var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

            //if (business is null)
            //    return (ReceivedInvoicesResult)ReceivedInvoicesResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            return await Task.FromResult(new PaginatedList<ReceivedInvoicesDto>([], 0, 1, 10)
            {
                 IsSuccess = true,
                 Message = "No Data Found",
                 StatusCodes = HttpStatusCodes.OK.ToInt()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get received invoices for business");
            return (PaginatedList<ReceivedInvoicesDto>)PaginatedList<ReceivedInvoicesDto>.Failure();
        }
    }

    private bool IsUserAuthorized() =>
       _currentUser.UserId.HasValue;
    //&& _currentUser.BusinessId.HasValue;
}
