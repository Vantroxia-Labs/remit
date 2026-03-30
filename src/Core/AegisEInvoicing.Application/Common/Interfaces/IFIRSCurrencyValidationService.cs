namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for validating currencies against FIRS API
/// </summary>
public interface IFIRSCurrencyValidationService
{
    /// <summary>
    /// Validates if a currency code is supported by FIRS
    /// </summary>
    Task<bool> IsValidCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets list of supported currency codes from FIRS
    /// </summary>
    Task<List<string>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default);
}