using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.DTOs;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.Commands.CreateInvoiceApprovalHistory;

public class CreateInvoiceApprovalHistoryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<CreateInvoiceApprovalHistoryCommandHandler> logger) : IRequestHandler<CreateInvoiceApprovalHistoryCommand, InvoiceApprovalHistoryResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<CreateInvoiceApprovalHistoryCommandHandler> _logger = logger;

    public async Task<InvoiceApprovalHistoryResult> Handle(CreateInvoiceApprovalHistoryCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
                return new InvoiceApprovalHistoryResult(false, "User authentication required");

            var approvalHistory = InvoiceApprovalHistory.Create(
              request.InvoiceId,
              request.InvoiceStatus,
              request.Comment);

            approvalHistory.MarkAsCreated(_currentUser.UserId.Value);

            var createdApprovalHistory = await _context.InvoiceApprovalHistories.AddAsync(approvalHistory, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        if (createdApprovalHistory is null)
        {
            return new InvoiceApprovalHistoryResult(false,
                           $"We could not add invoice approval history at this time. Please try again");
        }

        return new InvoiceApprovalHistoryResult(true,
                                                $"We have successfully added invoice approval history",
                                                approvalHistory.Id);
    }
}
