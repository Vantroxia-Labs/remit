using MediatR;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetAllLicenses;

/// <summary>
/// Query to get all OnPremise business licenses
/// Aegis Admin only
/// </summary>
public record GetAllLicensesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? Status = null, // "Active", "Expired", "ExpiringSoon", or null for all
    string? SearchTerm = null) : IRequest<GetAllLicensesResult>;
