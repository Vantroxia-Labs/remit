using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateInvoice;
using AegisEInvoicing.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Services;

/// <summary>
/// Service for efficiently validating invoice references in batch operations
/// </summary>
public class InvoiceReferenceValidator : IInvoiceReferenceValidator
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<InvoiceReferenceValidator> _logger;

    public InvoiceReferenceValidator(
        IApplicationDbContext context,
        ILogger<InvoiceReferenceValidator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool IsValid, string ErrorMessage)> ValidateIrnExistsAsync(
        string irn,
        DateOnly issueDate,
        Guid businessId,
        CancellationToken ct = default)
    {
        // Validate IRN format first (avoids database call for invalid formats)
        if (!IRN.IsValidIRNFormat(irn))
        {
            return (false, $"Invalid IRN format: '{irn}'. Expected format: PREFIXNNNNNNNN-SERVICEID-YYYYMMDD");
        }

        var irnValue = IRN.CreateFromString(irn);

        var exists = await _context.Invoices
            .AsNoTracking()
            .AnyAsync(i => i.BusinessId == businessId
                && i.Irn.Value == irnValue.Value
                && i.IssueDate == issueDate, ct);

        if (!exists)
        {
            return (false, $"Referenced IRN: '{irn}' is not tied to any previously created invoice");
        }

        return (true, string.Empty);
    }

    public async Task<Dictionary<string, bool>> ValidateMultipleIrnsAsync(
       IEnumerable<(string Irn, DateOnly IssueDate)> references,
       Guid businessId,
       CancellationToken ct = default)
    {
        var refList = references.ToList();
        if (!refList.Any())
            return new Dictionary<string, bool>();

        // Extract unique IRNs
        var irns = refList.Select(r => r.Irn).Distinct().ToList();

        // Fetch matching invoices in one query
        var existingInvoices = await _context.Invoices
            .AsNoTracking()
            .Where(i => i.BusinessId == businessId && irns.Contains(i.Irn.Value))
            .Select(i => new { i.Irn.Value, i.IssueDate })
            .ToListAsync(ct);

        // O(1) lookup: IRN + IssueDate combo
        var existingSet = existingInvoices
            .Select(i => $"{i.Value}|{i.IssueDate:yyyy-MM-dd}")
            .ToHashSet();

        var result = new Dictionary<string, bool>();

        foreach (var r in refList)
        {
            // Skip duplicates silently
            if (result.ContainsKey(r.Irn))
                continue;

            var key = $"{r.Irn}|{r.IssueDate:yyyy-MM-dd}";
            result[r.Irn] = existingSet.Contains(key);
        }

        return result;
    }

    public async Task<(bool IsValid, string ErrorMessage, List<string> InvalidReferences)> ValidateInvoiceReferencesAsync(
        List<CreateBillingReferenceDto>? billingReferences,
        CreateDocumentReferenceDto? dispatchReference,
        CreateDocumentReferenceDto? receiptReference,
        CreateDocumentReferenceDto? originatorReference,
        CreateDocumentReferenceDto? contractReference,
        List<CreateDocumentReferenceDto>? additionalReferences,
        Guid businessId,
        CancellationToken ct = default)
    {
        var allReferences = new List<(string Irn, DateOnly IssueDate)>();
        var invalidRefs = new List<string>();

        // Collect all references
        if (billingReferences != null)
        {
            foreach (var br in billingReferences)
            {
                if (!IRN.IsValidIRNFormat(br.Irn))
                {
                    invalidRefs.Add($"Billing Reference: Invalid IRN format '{br.Irn}'");
                    continue;
                }
                allReferences.Add((br.Irn, br.IssueDate));
            }
        }

        if (dispatchReference != null)
        {
            if (!IRN.IsValidIRNFormat(dispatchReference.Irn))
            {
                invalidRefs.Add($"Dispatch Reference: Invalid IRN format '{dispatchReference.Irn}'");
            }
            else
            {
                allReferences.Add((dispatchReference.Irn, dispatchReference.IssueDate));
            }
        }

        if (receiptReference != null)
        {
            if (!IRN.IsValidIRNFormat(receiptReference.Irn))
            {
                invalidRefs.Add($"Receipt Reference: Invalid IRN format '{receiptReference.Irn}'");
            }
            else
            {
                allReferences.Add((receiptReference.Irn, receiptReference.IssueDate));
            }
        }

        if (originatorReference != null)
        {
            if (!IRN.IsValidIRNFormat(originatorReference.Irn))
            {
                invalidRefs.Add($"Originator Reference: Invalid IRN format '{originatorReference.Irn}'");
            }
            else
            {
                allReferences.Add((originatorReference.Irn, originatorReference.IssueDate));
            }
        }

        if (contractReference != null)
        {
            if (!IRN.IsValidIRNFormat(contractReference.Irn))
            {
                invalidRefs.Add($"Contract Reference: Invalid IRN format '{contractReference.Irn}'");
            }
            else
            {
                allReferences.Add((contractReference.Irn, contractReference.IssueDate));
            }
        }

        if (additionalReferences != null)
        {
            foreach (var ar in additionalReferences)
            {
                if (!IRN.IsValidIRNFormat(ar.Irn))
                {
                    invalidRefs.Add($"Additional Reference: Invalid IRN format '{ar.Irn}'");
                    continue;
                }
                allReferences.Add((ar.Irn, ar.IssueDate));
            }
        }

        // If we have format errors, return early
        if (invalidRefs.Any())
        {
            return (false, string.Join("; ", invalidRefs), invalidRefs);
        }

        // If no references to validate, return valid
        if (!allReferences.Any())
        {
            return (true, string.Empty, new List<string>());
        }

        // Validate all references in single database query
        var validationResults = await ValidateMultipleIrnsAsync(allReferences, businessId, ct);

        // Check for non-existent references
        foreach (var kvp in validationResults.Where(kvp => !kvp.Value))
        {
            invalidRefs.Add($"Referenced IRN '{kvp.Key}' does not exist");
        }

        if (invalidRefs.Any())
        {
            return (false, string.Join("; ", invalidRefs), invalidRefs);
        }

        return (true, string.Empty, new List<string>());
    }
}
