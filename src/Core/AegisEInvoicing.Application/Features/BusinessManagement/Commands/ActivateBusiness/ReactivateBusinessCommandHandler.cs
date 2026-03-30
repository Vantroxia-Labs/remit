using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;
using AegisEInvoicing.NotificationService.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateBusiness;

public class ReactivateBusinessCommandHandler(IApplicationDbContext context,
    ICurrentUserService currentUser, IEmailService emailService, ILogger<ReactivateBusinessCommandHandler> logger) : IRequestHandler<ReactivateBusinessCommand, ReactivateBusinessResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<ReactivateBusinessCommandHandler> _logger = logger;

    public async Task<ReactivateBusinessResult> Handle(ReactivateBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue && !_currentUser.IsPlatformAdmin)
                return new ReactivateBusinessResult(false, "Invalid user authentication/permission");

            var getBusiness = await _context.Businesses.Include(s => s.Subscription).FirstOrDefaultAsync(b => b.Id == request.BusinessId, cancellationToken);

            if (getBusiness is null)
                return new ReactivateBusinessResult(false, "Business not found");

            var subscription = getBusiness.Subscription;

            subscription?.Activate(_currentUser.UserId.GetValueOrDefault());
            getBusiness.Activate(_currentUser.UserId);

            _context.Subscriptions.Update(subscription!);
            _context.Businesses.Update(getBusiness);

            await _context.SaveChangesAsync(cancellationToken);

            //Send email on business activation
            await _emailService.SendEmailAsync(new NotificationService.Models.EmailMessage
            {
                Subject = "Account Activation.",
                To = getBusiness.ContactEmail,
                TextBody = "Your business account has been activated successfully."
            });

            return new ReactivateBusinessResult(true, "Business activated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new ReactivateBusinessResult(false, "Something went wrong");
        }
    }
}
