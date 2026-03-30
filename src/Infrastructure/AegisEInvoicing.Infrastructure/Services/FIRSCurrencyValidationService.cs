using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Implementation of FIRS currency validation service with caching
/// Addresses currency validation against FIRS API
/// </summary>
public class FIRSCurrencyValidationService(
    IFIRSHttpClient firsClient,
    IMemoryCache cache,
    ILogger<FIRSCurrencyValidationService> logger) : IFIRSCurrencyValidationService
{
    private readonly IFIRSHttpClient _firsClient = firsClient ?? throw new ArgumentNullException(nameof(firsClient));
    private readonly IMemoryCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    private readonly ILogger<FIRSCurrencyValidationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private const string CacheKey = "FIRS_SUPPORTED_CURRENCIES";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(24);

    public async Task<bool> IsValidCurrencyAsync(string currencyCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return false;

        try
        {
            var supportedCurrencies = await GetSupportedCurrenciesAsync(cancellationToken);
            return supportedCurrencies.Contains(currencyCode, StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating currency {Currency} against FIRS API", currencyCode);

            // Fallback to NGN only if FIRS API fails
            return currencyCode.Equals("NGN", StringComparison.OrdinalIgnoreCase);
        }
    }

    public async Task<List<string>> GetSupportedCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_cache.TryGetValue<List<string>>(CacheKey, out var cachedCurrencies) && cachedCurrencies != null)
        {
            _logger.LogDebug("Retrieved {Count} supported currencies from cache", cachedCurrencies.Count);
            return cachedCurrencies;
        }

        try
        {
            _logger.LogInformation("Fetching supported currencies from FIRS API");

            var response = await _firsClient.GetCurrencies(cancellationToken);

            if (response?.Data == null || response.Data.Count == 0)
            {
                _logger.LogWarning("FIRS API returned no currencies, using fallback (NGN only)");
                return ["NGN"];
            }

            var currencies = response.Data
                .Select(c => c.Code)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Cache for 24 hours
            _cache.Set(CacheKey, currencies, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration,
                SlidingExpiration = TimeSpan.FromHours(6)
            });

            _logger.LogInformation("Cached {Count} supported currencies from FIRS API", currencies.Count);

            return currencies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch currencies from FIRS API, using fallback");

            // Fallback to NGN only
            return ["NGN"];
        }
    }
}