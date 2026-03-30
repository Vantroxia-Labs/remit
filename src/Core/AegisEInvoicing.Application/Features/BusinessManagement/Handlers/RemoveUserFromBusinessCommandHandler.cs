using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Handlers;

public class RemoveUserFromBusinessCommandHandler : IRequestHandler<RemoveUserFromBusinessCommand, RemoveUserFromBusinessResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RemoveUserFromBusinessCommandHandler> _logger;

    public RemoveUserFromBusinessCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        ILogger<RemoveUserFromBusinessCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RemoveUserFromBusinessResult> Handle(RemoveUserFromBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = _currentUserService.UserId
                ?? throw new InvalidOperationException("Current user ID is not available");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.BusinessId == request.BusinessId,
                    cancellationToken);

            if (user == null)
            {
                return new RemoveUserFromBusinessResult
                {
                    Success = false,
                    Message = "User not found in the specified business"
                };
            }

            // Deactivate the user rather than deleting
            user.Deactivate(currentUserId, "Removed from business");

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Removed user {UserId} from business {BusinessId}", 
                request.UserId, request.BusinessId);

            return new RemoveUserFromBusinessResult
            {
                Success = true,
                Message = "User removed from business successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing user {UserId} from business {BusinessId}", 
                request.UserId, request.BusinessId);
            return new RemoveUserFromBusinessResult
            {
                Success = false,
                Message = "Failed to remove user from business"
            };
        }
    }
}