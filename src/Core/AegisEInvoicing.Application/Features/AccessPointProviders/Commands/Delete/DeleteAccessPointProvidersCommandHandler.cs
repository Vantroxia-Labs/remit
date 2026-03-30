using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Delete;

public class DeleteAccessPointProvidersCommandHandler(IApplicationDbContext context,
    ICurrentUserService currentUser, IEncryptionService encryptionService, ILogger<DeleteAccessPointProvidersCommandHandler> logger) : IRequestHandler<DeleteAccessPointProvidersCommand, DeleteAccessPointProvidersResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<DeleteAccessPointProvidersCommandHandler> _logger = logger;
    public async Task<DeleteAccessPointProvidersResult> Handle(DeleteAccessPointProvidersCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue && !_currentUser.IsPlatformAdmin)
                return new DeleteAccessPointProvidersResult(false, "Invalid user authentication/permission");

            var getConfiguration = await _context.FIRSApiConfigurations.FirstOrDefaultAsync(f => f.Id == request.configurationId, cancellationToken);

            if (getConfiguration is null)
                return new DeleteAccessPointProvidersResult(false, $"FIRS configuration does not exists.");

            getConfiguration.MarkAsDeleted(_currentUser.UserId);

            await _context.SaveChangesAsync(cancellationToken);

            return new DeleteAccessPointProvidersResult(true, "FIRS configuration deleted successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new DeleteAccessPointProvidersResult(false, "Something went wrong.");
        }
    }
}
