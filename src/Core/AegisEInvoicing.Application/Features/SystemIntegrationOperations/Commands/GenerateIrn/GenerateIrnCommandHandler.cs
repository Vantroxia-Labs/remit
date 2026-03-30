using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.GenerateIrn;

public class GenerateIrnCommandHandler(
    IApplicationDbContext context,
    IHttpContextAccessor currentUser,
    ILogger<GenerateIrnCommandHandler> logger)
    : IRequestHandler<GenerateIrnCommand, GenerateIrnResult>
{
    private readonly IApplicationDbContext _context = context;
    private readonly IHttpContextAccessor _currentUser = currentUser;
    private readonly ILogger<GenerateIrnCommandHandler> _logger = logger;

    public async Task<GenerateIrnResult> Handle(GenerateIrnCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var businessId = TryGetBusinessId();
            if (businessId is null)
                return GenerateIrnResult.AuthorizationError();

            var business = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
            if (business is null)
                return GenerateIrnResult.NotFound(ResponseMessages.BUSINESS_NOT_FOUND);

            if (string.IsNullOrEmpty(business.ServiceId))
                return GenerateIrnResult.NotFound(ResponseMessages.SERVICE_ID_NOT_FOUND);

            var irn = IRN.Create(request.InvoiceNumber, business.ServiceId, request.IssueDate);
            _logger.LogInformation("System Integrator IRN for business, {buisnessName}, with Id, {businessId} successfully generated", business.Name, business.Id);
                return GenerateIrnResult.Successful(irn.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to Generate System Integrator IRN");
            return GenerateIrnResult.Failure(ResponseMessages.GENERATE_IRN_FAILED);
        }
    }

    private Guid? TryGetBusinessId()
    {
        var businessId = _currentUser.HttpContext?.User?.FindFirst("BusinessId")?.Value;
        return Guid.TryParse(businessId, out var result) ? result : null;
    }
}
