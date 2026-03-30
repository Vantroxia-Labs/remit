using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;

public class CreateAccessPointProvidersCommandHandler(IApplicationDbContext context,
    ICurrentUserService currentUser, IEncryptionService encryptionService, ILogger<CreateAccessPointProvidersCommandHandler> logger) : IRequestHandler<CreateAccessPointProvidersCommand, CreateAccessPointProvidersResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly ICurrentUserService _currentUser = currentUser;
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<CreateAccessPointProvidersCommandHandler> _logger = logger;

    public async Task<CreateAccessPointProvidersResult> Handle(CreateAccessPointProvidersCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue && !_currentUser.IsPlatformAdmin)
            return new CreateAccessPointProvidersResult(false, "Invalid user authentication/permission");

        string encryptedApiKey = await _encryptionService.EncryptAsync(request.ApiKey);
        string encryptedApiSecret = await _encryptionService.EncryptAsync(request.ApiSecret);

        var configuration = FIRSApiConfiguration.CreateForSaaS(request.Name, request.Description, encryptedApiKey, encryptedApiSecret, request.Environment, request.BaseUrl);

        await _context.FIRSApiConfigurations.AddAsync(configuration, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateAccessPointProvidersResult(true, "FIRS configuration request successful");
    }
}
