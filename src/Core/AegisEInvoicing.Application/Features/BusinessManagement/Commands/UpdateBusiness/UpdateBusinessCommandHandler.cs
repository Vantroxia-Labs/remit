using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.UpdateBusiness;

public class UpdateBusinessCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    ILogger<UpdateBusinessCommandHandler> logger) : IRequestHandler<UpdateBusinessCommand, UpdateBusinessResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly ILogger<UpdateBusinessCommandHandler> _logger = logger;

    public async Task<UpdateBusinessResult> Handle(UpdateBusinessCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue)
                return new UpdateBusinessResult(false, "User authentication required");

            var tempAdminId = Guid.CreateVersion7(); // Temporary ID

            var getBusiness = await _context.Businesses.FirstOrDefaultAsync(i => i.Id == request.BusinessId, cancellationToken);

            if (getBusiness is null)
                return new UpdateBusinessResult(false, $"Business does not exists.");

            getBusiness.Update(
                request.Description,
                request.InvoicePrefix,
                request.ContactEmail,
                request.RegisteredAddress,
                tempAdminId,
                request.ContactPhone,
                request.Industry,
                request.ServiceId,
                request.BusinessRegistrationNumber,
                !string.IsNullOrWhiteSpace(request.TaxIdentificationNumber) ? TIN.Create(request.TaxIdentificationNumber) : null,
                request.FIRSBusinessId);

            _context.Businesses.Update(getBusiness);
            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateBusinessResult(true, $"Updated business details successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new UpdateBusinessResult(
                false,
                $"Failed to onboard business: {ex.Message}");
        }
    }
}
