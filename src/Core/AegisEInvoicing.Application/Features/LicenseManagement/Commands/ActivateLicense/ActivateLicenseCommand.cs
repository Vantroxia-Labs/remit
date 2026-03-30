using MediatR;

namespace AegisEInvoicing.Application.Features.LicenseManagement.Commands.ActivateLicense;

/// <summary>
/// Command to activate a license key for the current business
/// Only OnPremise businesses can activate licenses
/// </summary>
public record ActivateLicenseCommand(string LicenseKey) : IRequest<ActivateLicenseResult>;
