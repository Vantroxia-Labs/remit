using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateBusiness;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.SuspendBusiness;

public class SuspendBusinessCommandHandler(IApplicationDbContext context,
    ICurrentUserService currentUser, IEmailService emailService, ILogger<ReactivateBusinessCommandHandler> logger) : IRequestHandler<SuspendBusinessCommand, SuspendBusinessResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<ReactivateBusinessCommandHandler> _logger = logger;

    public async Task<SuspendBusinessResult> Handle(SuspendBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue && !_currentUser.IsPlatformAdmin)
                return new SuspendBusinessResult(false, "Invalid user authentication/permission");

            var getBusiness = await _context.Businesses.Include(s => s.Subscriptions).FirstOrDefaultAsync(b => b.Id == request.BusinessId);

            if (getBusiness is null)
                return new SuspendBusinessResult(false, "Business not found");

            foreach (var sub in getBusiness.Subscriptions.Where(s => s.Status != Domain.Entities.BusinessManagement.SubscriptionStatus.Suspended))
            {
                sub.Suspend(_currentUser.UserId.GetValueOrDefault(), "Business deactivated by admin");
                _context.Subscriptions.Update(sub);
            }

            getBusiness.Deactivate(_currentUser.UserId);
            _context.Businesses.Update(getBusiness);

            await _context.SaveChangesAsync(cancellationToken);

            //Send email on business deactivation
            await _emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
            {
                Subject = "Account Deactivation.",
                To = getBusiness.ContactEmail,
                TextBody = "Your business account has been deactivated."
            });

            return new SuspendBusinessResult(true, "Business deactivated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new SuspendBusinessResult(false, "Something went wrong");
        }
    }
}
