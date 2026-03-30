using AegisEInvoicing.Infrastructure.Models;

namespace AegisEInvoicing.Infrastructure.Services.Interfaces;

public interface ILicenseValidationService
{
    Task<bool> ValidateLicenseAsync(CancellationToken cancellationToken = default);
    Task<LicenseInfo> GetLicenseInfoAsync(CancellationToken cancellationToken = default);
    bool IsLicenseValid();
    void ShutdownApplication(string reason);
}