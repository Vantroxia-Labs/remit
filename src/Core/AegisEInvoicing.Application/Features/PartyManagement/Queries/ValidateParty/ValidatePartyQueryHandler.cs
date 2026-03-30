using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.ValidateParty;

public class ValidatePartyQueryHandler : IRequestHandler<ValidatePartyQuery, Dictionary<string, bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<ValidatePartyQueryHandler> _logger;
    private static readonly HashSet<string> SupportedValidationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "TaxIdentificationNumber"
    };

    public ValidatePartyQueryHandler(IApplicationDbContext context,
        ICurrentUserService currentUser,
        ILogger<ValidatePartyQueryHandler> logger)
    {
        _context = context;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Dictionary<string, bool>> Handle(ValidatePartyQuery request, CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, bool>();

        if (!_currentUser.UserId.HasValue)
        {
            _logger.LogWarning("Unauthorized attempt to create party");
            throw new AuthenticationException("User authentication required");
        }

        if (!_currentUser.BusinessId.HasValue)
        {
            _logger.LogWarning("Business Not Found");
            throw new NotFoundException("Business Not Found");
        }

        foreach (var validationField in request.ValidationFields)
        {
            var validationType = validationField.Key;
            var value = validationField.Value;

            // Check if validation type is supported
            if (!SupportedValidationTypes.Contains(validationType))
            {
                results[validationType] = false;
                continue;
            }

            // Skip empty values
            if (string.IsNullOrWhiteSpace(value))
            {
                results[validationType] = false;
                continue;
            }

            bool exists = await ValidateFieldAsync(validationType, _currentUser.BusinessId.Value, value, cancellationToken);
            results[validationType] = exists;
        }

        return results;
    }

    private async Task<bool> ValidateFieldAsync(string validationType, Guid businessId, string value, CancellationToken cancellationToken)
    {
        return validationType.ToLowerInvariant() switch
        {
            "taxidentificationnumber" => await _context.Parties
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.BusinessID == businessId)
                .AnyAsync(b => b.TaxIdentificationNumber.Value == value, cancellationToken),
            _ => false
        };
    }
}
