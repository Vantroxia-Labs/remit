using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ToggleSftpInvoiceTransmission;

public sealed class ToggleSftpInvoiceTransmissionCommandHandler : IRequestHandler<ToggleSftpInvoiceTransmissionCommand, ToggleSftpInvoiceTransmissionResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ToggleSftpInvoiceTransmissionCommandHandler> _logger;

    public ToggleSftpInvoiceTransmissionCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<ToggleSftpInvoiceTransmissionCommandHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<ToggleSftpInvoiceTransmissionResult> Handle(ToggleSftpInvoiceTransmissionCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.BusinessId.HasValue)
        {
            return new ToggleSftpInvoiceTransmissionResult(false, "Business context is required", null, null);
        }

        var businessId = _currentUser.BusinessId.Value;

        var sftpUser = await _context.SFTPUsers.FirstOrDefaultAsync(u => u.BusinessId == businessId, cancellationToken);
        if (sftpUser is null)
        {
            _logger.LogWarning("SFTP user not found for business {BusinessId}", businessId);
            return new ToggleSftpInvoiceTransmissionResult(false, "SFTP user not found for this business", businessId, null);
        }
       
        var actor = _currentUser.UserId ?? Guid.Empty;
        if (request.Enabled)
            sftpUser.EnableInvoiceTransmission(actor);
        else
            sftpUser.DisableInvoiceTransmission(actor);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SFTP invoice transmission set to {Enabled} for business {BusinessId}", request.Enabled, businessId);
        return new ToggleSftpInvoiceTransmissionResult(true, "SFTP invoice transmission toggle updated", businessId, sftpUser.SftpInvoiceTransmissionEnabled);
    }
}
