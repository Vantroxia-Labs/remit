using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.FIRSAccessPoint.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AegisEInvoicing.Infrastructure.Services.Caching;

/// <summary>
/// Caches FIRS reference data to avoid repeated API calls
/// Thread-safe with ConcurrentDictionary
/// Auto-refreshes daily at 12:00 AM via background service
/// Uses IServiceScopeFactory to resolve scoped IFIRSHttpClient from singleton service
/// </summary>
public class ReferenceDataCacheService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReferenceDataCacheService> logger) : IReferenceDataCacheService
{
    private readonly ConcurrentDictionary<string, string> _invoiceTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _currencies = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _paymentMeans = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _serviceCodes = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> _taxCategories = new(StringComparer.OrdinalIgnoreCase);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private DateTime _lastRefreshTime = DateTime.MinValue;
    
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    private readonly ILogger<ReferenceDataCacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private const int CacheExpirationHours = 24;

    public bool IsValidInvoiceType(string typeCode)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
            return false;

        return _invoiceTypes.ContainsKey(typeCode);
    }

    public bool IsValidCurrency(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return false;

        return _currencies.ContainsKey(currencyCode);
    }

    public bool IsValidPaymentMeans(string paymentMeansCode)
    {
        if (string.IsNullOrWhiteSpace(paymentMeansCode))
            return false;

        return _paymentMeans.ContainsKey(paymentMeansCode);
    }

    public bool IsValidServiceCode(string serviceCode)
    {
        if (string.IsNullOrWhiteSpace(serviceCode))
            return false;

        return _serviceCodes.ContainsKey(serviceCode);
    }

    public bool IsValidTaxCategory(string taxCategoryCode)
    {
        if (string.IsNullOrWhiteSpace(taxCategoryCode))
            return false;

        return _taxCategories.ContainsKey(taxCategoryCode);
    }

    public IReadOnlyList<string> GetInvoiceTypeCodes() => _invoiceTypes.Keys.ToList();

    public string? GetInvoiceTypeName(string typeCode) =>
        _invoiceTypes.TryGetValue(typeCode, out var name) ? name : null;

    public IReadOnlyList<string> GetCurrencyCodes() => _currencies.Keys.ToList();

    public string? GetCurrencyName(string currencyCode) =>
        _currencies.TryGetValue(currencyCode, out var name) ? name : null;

    public IReadOnlyList<string> GetPaymentMeansCodes() => _paymentMeans.Keys.ToList();

    public string? GetPaymentMeansName(string paymentMeansCode) =>
        _paymentMeans.TryGetValue(paymentMeansCode, out var name) ? name : null;

    public IReadOnlyList<string> GetServiceCodes() => _serviceCodes.Keys.ToList();

    public string? GetServiceCodeName(string serviceCode) =>
        _serviceCodes.TryGetValue(serviceCode, out var name) ? name : null;

    public IReadOnlyList<string> GetTaxCategoryCodes() => _taxCategories.Keys.ToList();

    public string? GetTaxCategoryName(string taxCategoryCode) =>
        _taxCategories.TryGetValue(taxCategoryCode, out var name) ? name : null;

    public async Task RefreshCacheAsync(CancellationToken cancellationToken = default)
    {
        var requestId = Guid.NewGuid();
        var startTime = DateTime.UtcNow;
        
        await _refreshLock.WaitAsync(cancellationToken);
        
        // Declare telemetryService outside try-catch so it's available in catch block
        ITelemetryService? telemetryService = null;
        
        try
        {
            _logger.LogInformation(
                "[{RequestId}] Starting FIRS reference data cache refresh. Current cache age: {CacheAge} hours",
                requestId, (DateTime.UtcNow - _lastRefreshTime).TotalHours);

            // Create a scope to resolve scoped IFIRSHttpClient
            using var scope = _scopeFactory.CreateScope();
            var firsClient = scope.ServiceProvider.GetRequiredService<IFIRSHttpClient>();
            
            // Resolve ITelemetryService (optional - may be null)
            telemetryService = scope.ServiceProvider.GetService<ITelemetryService>();

            // ATOMIC REFRESH: Load into TEMPORARY dictionaries first
            var tempInvoiceTypes = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tempCurrencies = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tempPaymentMeans = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tempServiceCodes = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var tempTaxCategories = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Track which refreshes succeeded/failed
            var refreshResults = new Dictionary<string, bool>();

            // Fetch all data in parallel - each method handles its own exceptions
            await Task.WhenAll(
                RefreshInvoiceTypesAsync(firsClient, tempInvoiceTypes, refreshResults, cancellationToken),
                RefreshCurrenciesAsync(firsClient, tempCurrencies, refreshResults, cancellationToken),
                RefreshPaymentMeansAsync(firsClient, tempPaymentMeans, refreshResults, cancellationToken),
                RefreshServiceCodesAsync(firsClient, tempServiceCodes, refreshResults, cancellationToken),
                RefreshTaxCategoriesAsync(firsClient, tempTaxCategories, refreshResults, cancellationToken)
            );

            // Log refresh results summary
            var successCount = refreshResults.Count(r => r.Value);
            var failedCount = refreshResults.Count(r => !r.Value);
            
            _logger.LogInformation(
                "[{RequestId}] FIRS API calls completed. Success: {SuccessCount}/5, Failed: {FailedCount}/5. " +
                "Results: {Results}",
                requestId, successCount, failedCount,
                string.Join(", ", refreshResults.Select(r => $"{r.Key}={r.Value}")));

            // Check if we got ANY useful data
            var hasAnyData = !tempInvoiceTypes.IsEmpty || !tempCurrencies.IsEmpty || 
                             !tempServiceCodes.IsEmpty || !tempTaxCategories.IsEmpty ||
                             !tempPaymentMeans.IsEmpty;

            if (!hasAnyData)
            {
                throw new InvalidOperationException(
                    $"[{requestId}] FIRS API returned no reference data at all. " +
                    $"All 5 refresh attempts failed. Check FIRS API connectivity.");
            }

            // Log warning if partial data
            if (failedCount > 0)
            {
                _logger.LogWarning(
                    "[{RequestId}] Partial cache refresh - some FIRS API calls failed. " +
                    "InvoiceTypes: {InvoiceTypes}, Currencies: {Currencies}, " +
                    "PaymentMeans: {PaymentMeans}, ServiceCodes: {ServiceCodes}, TaxCategories: {TaxCategories}",
                    requestId,
                    tempInvoiceTypes.Count, tempCurrencies.Count,
                    tempPaymentMeans.Count, tempServiceCodes.Count, tempTaxCategories.Count);
            }

            // ATOMIC SWAP: Only update caches that have new data
            // Don't clear production cache if temp is empty (preserve existing data)
            
            if (!tempInvoiceTypes.IsEmpty)
            {
                _invoiceTypes.Clear();
                foreach (var kvp in tempInvoiceTypes)
                    _invoiceTypes.TryAdd(kvp.Key, kvp.Value);
            }
            else
            {
                _logger.LogWarning("[{RequestId}] Keeping existing invoice types cache (new fetch was empty)", requestId);
            }
            
            if (!tempCurrencies.IsEmpty)
            {
                _currencies.Clear();
                foreach (var kvp in tempCurrencies)
                    _currencies.TryAdd(kvp.Key, kvp.Value);
            }
            else
            {
                _logger.LogWarning("[{RequestId}] Keeping existing currencies cache (new fetch was empty)", requestId);
            }
            
            if (!tempPaymentMeans.IsEmpty)
            {
                _paymentMeans.Clear();
                foreach (var kvp in tempPaymentMeans)
                    _paymentMeans.TryAdd(kvp.Key, kvp.Value);
            }
            else
            {
                _logger.LogWarning("[{RequestId}] Keeping existing payment means cache (new fetch was empty)", requestId);
            }
            
            if (!tempServiceCodes.IsEmpty)
            {
                _serviceCodes.Clear();
                foreach (var kvp in tempServiceCodes)
                    _serviceCodes.TryAdd(kvp.Key, kvp.Value);
            }
            else
            {
                _logger.LogWarning("[{RequestId}] Keeping existing service codes cache (new fetch was empty)", requestId);
            }
            
            if (!tempTaxCategories.IsEmpty)
            {
                _taxCategories.Clear();
                foreach (var kvp in tempTaxCategories)
                    _taxCategories.TryAdd(kvp.Key, kvp.Value);
            }
            else
            {
                _logger.LogWarning("[{RequestId}] Keeping existing tax categories cache (new fetch was empty)", requestId);
            }

            _lastRefreshTime = DateTime.UtcNow;

            var duration = DateTime.UtcNow - startTime;

            _logger.LogInformation(
                "[{RequestId}] Cache refreshed successfully in {Duration}ms. " +
                "InvoiceTypes: {InvoiceTypes}, Currencies: {Currencies}, " +
                "PaymentMeans: {PaymentMeans}, ServiceCodes: {ServiceCodes}, TaxCategories: {TaxCategories}",
                requestId, duration.TotalMilliseconds,
                _invoiceTypes.Count, _currencies.Count, _paymentMeans.Count,
                _serviceCodes.Count, _taxCategories.Count);

            // Track successful cache refresh
            telemetryService?.TrackEvent("ReferenceDataCacheRefreshed", 
                new Dictionary<string, string>
                {
                    ["RequestId"] = requestId.ToString(),
                    ["InvoiceTypesCount"] = _invoiceTypes.Count.ToString(),
                    ["CurrenciesCount"] = _currencies.Count.ToString(),
                    ["PaymentMeansCount"] = _paymentMeans.Count.ToString(),
                    ["ServiceCodesCount"] = _serviceCodes.Count.ToString(),
                    ["TaxCategoriesCount"] = _taxCategories.Count.ToString()
                },
                new Dictionary<string, double>
                {
                    ["RefreshDurationMs"] = duration.TotalMilliseconds
                });
        }
        catch (Exception ex)
        {
            var cacheAge = DateTime.UtcNow - _lastRefreshTime;
            
            _logger.LogWarning(ex,
                "[{RequestId}] Cache refresh FAILED. Keeping existing cache from {LastRefresh}. " +
                "Cache age: {CacheAgeHours:F1} hours. " +
                "Current counts - InvoiceTypes: {InvoiceTypes}, Currencies: {Currencies}",
                requestId, _lastRefreshTime, cacheAge.TotalHours,
                _invoiceTypes.Count, _currencies.Count);

            // CRITICAL: Alert if cache is getting stale (>7 days)
            if (cacheAge.TotalDays > 7)
            {
                _logger.LogCritical(
                    "[{RequestId}] ALERT: Reference data cache is {CacheAgeDays:F1} days old. " +
                    "FIRS API may be experiencing extended downtime. Manual intervention required.",
                    requestId, cacheAge.TotalDays);
            }
            else if (cacheAge.TotalDays > 3)
            {
                _logger.LogWarning(
                    "[{RequestId}] WARNING: Reference data cache is {CacheAgeDays:F1} days old. " +
                    "Monitoring FIRS API availability.",
                    requestId, cacheAge.TotalDays);
            }

            // Track cache refresh failure
            telemetryService?.TrackEvent("ReferenceDataCacheRefreshFailed", 
                new Dictionary<string, string>
                {
                    ["RequestId"] = requestId.ToString(),
                    ["ErrorMessage"] = ex.Message,
                    ["CacheAgeHours"] = cacheAge.TotalHours.ToString("F1")
                });
            
            // For startup (first load), propagate exception
            if (_lastRefreshTime == DateTime.MinValue)
            {
                _logger.LogCritical(
                    "[{RequestId}] Initial cache load FAILED. Cannot start application without reference data.",
                    requestId);
                throw;
            }

            // For scheduled refresh, swallow exception (keep using stale cache)
            _logger.LogInformation(
                "[{RequestId}] Continuing with existing cache. Next refresh will be attempted on schedule.",
                requestId);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public DateTime GetLastRefreshTime() => _lastRefreshTime;

    public bool IsCacheHealthy()
    {
        if (_lastRefreshTime == DateTime.MinValue)
            return false;

        var cacheAge = DateTime.UtcNow - _lastRefreshTime;
        if (cacheAge.TotalHours > CacheExpirationHours)
            return false;

        return _invoiceTypes.Count > 0 && 
               _currencies.Count > 0 && 
               _paymentMeans.Count > 0 && 
               _serviceCodes.Count > 0 && 
               _taxCategories.Count > 0;
    }

    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            InvoiceTypesCount = _invoiceTypes.Count,
            CurrenciesCount = _currencies.Count,
            PaymentMeansCount = _paymentMeans.Count,
            ServiceCodesCount = _serviceCodes.Count,
            TaxCategoriesCount = _taxCategories.Count,
            LastRefreshTime = _lastRefreshTime,
            IsHealthy = IsCacheHealthy(),
            TimeSinceRefresh = DateTime.UtcNow - _lastRefreshTime
        };
    }

    private async Task RefreshInvoiceTypesAsync(
        IFIRSHttpClient firsClient,
        ConcurrentDictionary<string, string> targetDictionary,
        Dictionary<string, bool> results,
        CancellationToken cancellationToken)
    {
        const string refreshName = "InvoiceTypes";
        try
        {
            var response = await firsClient.GetInvoiceType(cancellationToken);
            
            if (response?.Data != null && response.Data.Any())
            {
                foreach (var type in response.Data)
                {
                    if (!string.IsNullOrWhiteSpace(type.Code))
                    {
                        targetDictionary.TryAdd(type.Code, type.Value ?? type.Code);
                    }
                }

                _logger.LogDebug("Fetched {Count} invoice types from FIRS", targetDictionary.Count);
                lock (results) { results[refreshName] = true; }
            }
            else
            {
                _logger.LogWarning("FIRS returned empty or null invoice types response");
                lock (results) { results[refreshName] = false; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch invoice types from FIRS - will use existing cache");
            lock (results) { results[refreshName] = false; }
            // Don't throw - fail silently
        }
    }

    private async Task RefreshCurrenciesAsync(
        IFIRSHttpClient firsClient,
        ConcurrentDictionary<string, string> targetDictionary,
        Dictionary<string, bool> results,
        CancellationToken cancellationToken)
    {
        const string refreshName = "Currencies";
        try
        {
            var response = await firsClient.GetCurrencies(cancellationToken);
            
            if (response?.Data != null && response.Data.Any())
            {
                foreach (var currency in response.Data)
                {
                    if (!string.IsNullOrWhiteSpace(currency.Code))
                    {
                        targetDictionary.TryAdd(currency.Code, currency.Name ?? currency.Code);
                    }
                }

                _logger.LogDebug("Fetched {Count} currencies from FIRS", targetDictionary.Count);
                lock (results) { results[refreshName] = true; }
            }
            else
            {
                _logger.LogWarning("FIRS returned empty or null currencies response");
                lock (results) { results[refreshName] = false; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch currencies from FIRS - will use existing cache");
            lock (results) { results[refreshName] = false; }
            // Don't throw - fail silently
        }
    }

    private async Task RefreshPaymentMeansAsync(
        IFIRSHttpClient firsClient,
        ConcurrentDictionary<string, string> targetDictionary,
        Dictionary<string, bool> results,
        CancellationToken cancellationToken)
    {
        const string refreshName = "PaymentMeans";
        try
        {
            var response = await firsClient.GetPaymentMeans(cancellationToken);
            
            if (response?.Data != null && response.Data.Any())
            {
                foreach (var means in response.Data)
                {
                    if (!string.IsNullOrWhiteSpace(means.Code))
                    {
                        targetDictionary.TryAdd(means.Code, means.Value ?? means.Code);
                    }
                }

                _logger.LogDebug("Fetched {Count} payment means from FIRS", targetDictionary.Count);
                lock (results) { results[refreshName] = true; }
            }
            else
            {
                _logger.LogWarning("FIRS returned empty or null payment means response");
                lock (results) { results[refreshName] = false; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch payment means from FIRS - will use existing cache");
            lock (results) { results[refreshName] = false; }
            // Don't throw - fail silently
        }
    }

    private async Task RefreshServiceCodesAsync(
        IFIRSHttpClient firsClient,
        ConcurrentDictionary<string, string> targetDictionary,
        Dictionary<string, bool> results,
        CancellationToken cancellationToken)
    {
        const string refreshName = "ServiceCodes";
        try
        {
            var response = await firsClient.GetServiceCodes(cancellationToken);
            
            if (response?.Data != null && response.Data.Any())
            {
                foreach (var service in response.Data)
                {
                    if (!string.IsNullOrWhiteSpace(service.Code))
                    {
                        targetDictionary.TryAdd(service.Code, service.Description ?? service.Code);
                    }
                }

                _logger.LogDebug("Fetched {Count} service codes from FIRS", targetDictionary.Count);
                lock (results) { results[refreshName] = true; }
            }
            else
            {
                _logger.LogWarning("FIRS returned empty or null service codes response");
                lock (results) { results[refreshName] = false; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch service codes from FIRS - will use existing cache");
            lock (results) { results[refreshName] = false; }
            // Don't throw - fail silently
        }
    }

    private async Task RefreshTaxCategoriesAsync(
        IFIRSHttpClient firsClient,
        ConcurrentDictionary<string, string> targetDictionary,
        Dictionary<string, bool> results,
        CancellationToken cancellationToken)
    {
        const string refreshName = "TaxCategories";
        try
        {
            var response = await firsClient.GetTaxCategories(cancellationToken);
            
            if (response?.Data != null && response.Data.Any())
            {
                foreach (var category in response.Data)
                {
                    if (!string.IsNullOrWhiteSpace(category.Code))
                    {
                        targetDictionary.TryAdd(category.Code, category.Value ?? category.Code);
                    }
                }

                _logger.LogDebug("Fetched {Count} tax categories from FIRS", targetDictionary.Count);
                lock (results) { results[refreshName] = true; }
            }
            else
            {
                _logger.LogWarning("FIRS returned empty or null tax categories response");
                lock (results) { results[refreshName] = false; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch tax categories from FIRS - will use existing cache");
            lock (results) { results[refreshName] = false; }
            // Don't throw - fail silently
        }
    }
}
