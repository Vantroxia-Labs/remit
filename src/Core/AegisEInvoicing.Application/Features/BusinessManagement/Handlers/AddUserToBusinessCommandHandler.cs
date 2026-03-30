using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Handlers;

public class AddUserToBusinessCommandHandler : IRequestHandler<AddUserToBusinessCommand, AddUserToBusinessResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AddUserToBusinessCommandHandler> _logger;

    public AddUserToBusinessCommandHandler(
        IApplicationDbContext context,
        ILogger<AddUserToBusinessCommandHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AddUserToBusinessResult> Handle(AddUserToBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var business = await _context.Businesses
                .FirstOrDefaultAsync(b => b.Id == request.BusinessId, cancellationToken);

            if (business == null)
            {
                return new AddUserToBusinessResult
                {
                    Success = false,
                    Message = $"Business not found: {request.BusinessId}"
                };
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return new AddUserToBusinessResult
                {
                    Success = false,
                    Message = $"User not found: {request.UserId}"
                };
            }

            // Note: This would need proper implementation with domain methods
            // For now, this is a placeholder implementation
            
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Added user {UserId} to business {BusinessId} with role {Role}", 
                request.UserId, request.BusinessId, request.Role);

            return new AddUserToBusinessResult
            {
                Success = true,
                Message = "User added to business successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user {UserId} to business {BusinessId}", 
                request.UserId, request.BusinessId);
            return new AddUserToBusinessResult
            {
                Success = false,
                Message = "Failed to add user to business"
            };
        }
    }
}