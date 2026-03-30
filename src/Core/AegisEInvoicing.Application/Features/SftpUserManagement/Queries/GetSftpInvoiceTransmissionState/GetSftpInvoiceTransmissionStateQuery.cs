using MediatR;
using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Queries.GetSftpInvoiceTransmissionState;

public sealed record GetSftpInvoiceTransmissionStateQuery() : IRequest<SftpInvoiceTransmissionStateDto?>;

public sealed class SftpInvoiceTransmissionStateDto
{
    public Guid BusinessId { get; set; }
    public bool Enabled { get; set; }
}

public sealed class GetSftpInvoiceTransmissionStateQueryHandler : IRequestHandler<GetSftpInvoiceTransmissionStateQuery, SftpInvoiceTransmissionStateDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<GetSftpInvoiceTransmissionStateQueryHandler> _logger;

    public GetSftpInvoiceTransmissionStateQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<GetSftpInvoiceTransmissionStateQueryHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<SftpInvoiceTransmissionStateDto?> Handle(GetSftpInvoiceTransmissionStateQuery request, CancellationToken cancellationToken)
    {
        if (!_currentUser.BusinessId.HasValue)
        {
            _logger.LogWarning("Business context is required to fetch SFTP invoice transmission state");
            return null;
        }

        var businessId = _currentUser.BusinessId.Value;
        var sftpUser = await _context.SFTPUsers.AsNoTracking().FirstOrDefaultAsync(u => u.BusinessId == businessId, cancellationToken);
        if (sftpUser is null)
        {
            _logger.LogWarning("SFTP user not found for business {BusinessId}", businessId);
            return null;
        }

        return new SftpInvoiceTransmissionStateDto
        {
            BusinessId = businessId,
            Enabled = sftpUser.SftpInvoiceTransmissionEnabled
        };
    }
}
