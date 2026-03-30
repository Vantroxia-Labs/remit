using MediatR;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Queries.GetLicenseHistory;

/// <summary>
/// Query to get current business's license information with pagination and filtering
/// Client Admin only - sees only their own business license
/// </summary>
public record GetLicenseHistoryQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Status = null,      // Filter: "Active", "Expired", "ExpiringSoon", "NotActivated"
    string? SearchTerm = null)  // Search in license key
    : IRequest<GetLicenseHistoryResult>;


