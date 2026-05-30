using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.UpdateBusiness;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.CancelSubscription;

public class CancelSubscriptionCommandHandler(IApplicationDbContext context,
    ICurrentUserService currentUser) : IRequestHandler<CancelSubscriptionCommand, CancelSubscriptionResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;

    public async Task<CancelSubscriptionResult> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {

        try
        {
            if (!_currentUser.BusinessId.HasValue)
                return new CancelSubscriptionResult(false, "User authentication required");

            var getBusiness = await _context.Businesses.Include(s => s.Subscriptions).FirstOrDefaultAsync(i => i.Id == _currentUser.BusinessId);

            if(getBusiness is null)
                return new CancelSubscriptionResult(false, "Business does not exist.");

            var reason = string.IsNullOrWhiteSpace(request.reason) ? "Cancelled by Merchant Admin" : request.reason;
            foreach (var sub in getBusiness.Subscriptions.Where(s => s.Status != AegisEInvoicing.Domain.Entities.BusinessManagement.SubscriptionStatus.Cancelled))
            {
                sub.Cancel(_currentUser.UserId.GetValueOrDefault(), reason);
                _context.Subscriptions.Update(sub);
            }
            await _context.SaveChangesAsync(cancellationToken);

            return new CancelSubscriptionResult(true, $"Subscription updated successfully.");
        }
        catch (Exception ex)
        {
            return new CancelSubscriptionResult(false, $"Something went wrong. Failed to cancel subscription: {ex.Message}");
        }
    }
}
