namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for caching FIRS reference data to avoid repeated API calls
/// Cache is refreshed daily at 12:00 PM
/// </summary>
public interface IReferenceDataCacheService
{
    /// <summary>
    /// Validates if invoice type code exists in FIRS
    /// </summary>
    bool IsValidInvoiceType(string typeCode);

    /// <summary>
    /// Validates if currency code exists in FIRS
    /// </summary>
    bool IsValidCurrency(string currencyCode);

    /// <summary>
    /// Validates if payment means code exists in FIRS
    /// </summary>
    bool IsValidPaymentMeans(string paymentMeansCode);

    /// <summary>
    /// Validates if service code exists in FIRS
    /// </summary>
    bool IsValidServiceCode(string serviceCode);

    /// <summary>
    /// Validates if tax category code exists in FIRS
    /// </summary>
    bool IsValidTaxCategory(string taxCategoryCode);

    /// <summary>
    /// Gets all cached invoice types
    /// </summary>
    IReadOnlyList<string> GetInvoiceTypeCodes();

    /// <summary>
    /// Gets the display name for an invoice type code, or null if not found in cache
    /// </summary>
    string? GetInvoiceTypeName(string typeCode);

    /// <summary>
    /// Gets all cached currency codes
    /// </summary>
    IReadOnlyList<string> GetCurrencyCodes();

    /// <summary>
    /// Gets the display name for a currency code, or null if not found in cache
    /// </summary>
    string? GetCurrencyName(string currencyCode);

    /// <summary>
    /// Gets all cached payment means codes
    /// </summary>
    IReadOnlyList<string> GetPaymentMeansCodes();

    /// <summary>
    /// Gets the display name for a payment means code, or null if not found in cache
    /// </summary>
    string? GetPaymentMeansName(string paymentMeansCode);

    /// <summary>
    /// Gets all cached service codes
    /// </summary>
    IReadOnlyList<string> GetServiceCodes();

    /// <summary>
    /// Gets the display name for a service code, or null if not found in cache
    /// </summary>
    string? GetServiceCodeName(string serviceCode);

    /// <summary>
    /// Gets all cached tax category codes
    /// </summary>
    IReadOnlyList<string> GetTaxCategoryCodes();

    /// <summary>
    /// Gets the display name for a tax category code, or null if not found in cache
    /// </summary>
    string? GetTaxCategoryName(string taxCategoryCode);

    /// <summary>
    /// Manually refreshes the cache from FIRS API
    /// </summary>
    Task RefreshCacheAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last time cache was refreshed
    /// </summary>
    DateTime GetLastRefreshTime();

    /// <summary>
    /// Checks if cache is healthy (has data and not expired)
    /// </summary>
    bool IsCacheHealthy();

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    CacheStatistics GetStatistics();
}

/// <summary>
/// Cache statistics for monitoring
/// </summary>
public record CacheStatistics
{
    public int InvoiceTypesCount { get; init; }
    public int CurrenciesCount { get; init; }
    public int PaymentMeansCount { get; init; }
    public int ServiceCodesCount { get; init; }
    public int TaxCategoriesCount { get; init; }
    public DateTime LastRefreshTime { get; init; }
    public bool IsHealthy { get; init; }
    public TimeSpan TimeSinceRefresh { get; init; }
}
