using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Update;

public class UpdateAccessPointProvidersCommandHandler(IApplicationDbContext context,
    ICurrentUserService currentUser, IEncryptionService encryptionService, ILogger<UpdateAccessPointProvidersCommandHandler> logger) : IRequestHandler<UpdateAccessPointProvidersCommand, UpdateAccessPointProvidersResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<UpdateAccessPointProvidersCommandHandler> _logger = logger;
    public async Task<UpdateAccessPointProvidersResult> Handle(UpdateAccessPointProvidersCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!_currentUser.UserId.HasValue && !_currentUser.IsPlatformAdmin)
                return new UpdateAccessPointProvidersResult(false, "Invalid user authentication/permission");

            string encryptedApiKey = await _encryptionService.EncryptAsync(request.apiKey);
            string encryptedApiSecret = await _encryptionService.EncryptAsync(request.apiSecret);

            var getConfiguration = await _context.FIRSApiConfigurations.FirstOrDefaultAsync(f => f.Id == request.configurationId, cancellationToken);

            if (getConfiguration is null)
                return new UpdateAccessPointProvidersResult(false, $"FIRS configuration does not exists.");

            getConfiguration.UpdateCredentials(request.name, request.description, encryptedApiKey, encryptedApiSecret, request.env, request.baseUrl);

            _context.FIRSApiConfigurations.Update(getConfiguration);
            await _context.SaveChangesAsync(cancellationToken);

            return new UpdateAccessPointProvidersResult(true, "FIRS configuration updated successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new UpdateAccessPointProvidersResult(false, "Something went wrong.");
        }
    }
}
