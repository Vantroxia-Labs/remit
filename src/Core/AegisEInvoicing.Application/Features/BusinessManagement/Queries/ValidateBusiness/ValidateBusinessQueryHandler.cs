using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.ValidateBusiness;

public class ValidateBusinessQueryHandler : IRequestHandler<ValidateBusinessQuery, Dictionary<string, bool>>
{
    private readonly IApplicationDbContext _context;
    private static readonly HashSet<string> SupportedValidationTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "ServiceId",
        "BusinessRegistrationNumber",
        "TaxIdentificationNumber",
        "AdminEmail",
        "ContactEmail",
        "FIRSBusinessId",
        "BusinessName"

    };

    public ValidateBusinessQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, bool>> Handle(ValidateBusinessQuery request, CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, bool>();

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

            bool exists = await ValidateFieldAsync(validationType, value, cancellationToken);
            results[validationType] = exists;
        }

        return results;
    }

    private async Task<bool> ValidateFieldAsync(string validationType, string value, CancellationToken cancellationToken)
    {
        return validationType.ToLowerInvariant() switch
        {
            "serviceid" => await _context.Businesses
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .AnyAsync(b => b.ServiceId == value, cancellationToken),

            "businessregistrationnumber" => await _context.Businesses
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .AnyAsync(b => b.BusinessRegistrationNumber == value, cancellationToken),

            "taxidentificationnumber" => await _context.Businesses
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .AnyAsync(b => b.TaxIdentificationNumber.Value == value, cancellationToken),
            "adminemail" => await _context.Users
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .AnyAsync(b => b.Email == value, cancellationToken),
            "contactemail" => await _context.Businesses
                .AsNoTracking()
                .Where(b => !b.IsDeleted)
                .AnyAsync(b => b.ContactEmail == value, cancellationToken),
            "firsbusinessid" => await _context.Businesses
           .AsNoTracking()
           .Where(b => !b.IsDeleted)
           .AnyAsync(b => b.FIRSBusinessId.ToString() == value, cancellationToken),

            "businessname" => await _context.Businesses
         .AsNoTracking()
         .Where(b => !b.IsDeleted)
         .AnyAsync(b => b.Name == value, cancellationToken),
            _ => false
        };
    }
}
