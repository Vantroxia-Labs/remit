using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace AegisEInvoicing.ERP.API.Middleware;

public class ApiUsageRequestData
{
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public long ResponseTimeMs { get; set; }
    public long RequestSize { get; set; }
    public long ResponseSize { get; set; }
    public string? RemoteIpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Guid? UserId { get; set; }
    public Guid? BusinessId { get; set; }
    public string? ApiKey { get; set; }
    public IServiceProvider ServiceProvider { get; set; } = null!;
}

public class ApiUsageTrackingMiddleware(
    RequestDelegate next,
    ILogger<ApiUsageTrackingMiddleware> logger,
    IConfiguration configuration)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ApiUsageTrackingMiddleware> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_configuration.GetValue<bool>("ApiUsageTracking:Enabled"))
        {
            await _next(context);
            return;
        }

        // Skip tracking for non-API endpoints
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;
        long requestSize = 0;
        long responseSize = 0;

        try
        {
            // Calculate request size
            if (context.Request.ContentLength.HasValue)
            {
                requestSize = context.Request.ContentLength.Value;
            }

            // Capture response size if enabled
            if (_configuration.GetValue<bool>("ApiUsageTracking:TrackResponseSize"))
            {
                using var responseBody = new MemoryStream();
                context.Response.Body = responseBody;

                await _next(context);

                responseSize = responseBody.Length;
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
            else
            {
                await _next(context);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
            stopwatch.Stop();

            // Extract data from HttpContext before it gets disposed
            var requestData = new ApiUsageRequestData
            {
                Path = context.Request.Path.Value ?? "",
                Method = context.Request.Method,
                StatusCode = context.Response.StatusCode,
                ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                RequestSize = requestSize,
                ResponseSize = responseSize,
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                UserId = GetUserIdFromClaims(context.User),
                BusinessId = GetBusinessIdFromClaims(context.User),
                ApiKey = GetApiKeyFromRequest(context),
                ServiceProvider = context.RequestServices
            };

            // Record usage asynchronously with extracted data
            _ = Task.Run(async () =>
            {
                try
                {
                    await RecordApiUsageAsync(requestData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to record API usage");
                }
            });
        }
    }

    private async Task RecordApiUsageAsync(ApiUsageRequestData requestData)
    {
        using var scope = requestData.ServiceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Get user and business information from the request data
        var userId = requestData.UserId;
        var businessId = requestData.BusinessId;
        var apiKey = requestData.ApiKey;

        if (businessId is null && string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No business ID or API key found for request tracking");
            return;
        }

        // If API key is provided, get business from API key
        if (!string.IsNullOrEmpty(apiKey) && businessId == null)
        {
            businessId = await GetBusinessIdFromApiKeyAsync(dbContext, apiKey);
        }

        if (businessId == null)
        {
            _logger.LogWarning("Could not determine business for API usage tracking");
            return;
        }

        var usageRecord = ApiUsageTracking.Create(
            businessId.Value,
            requestData.Path,
            requestData.Method,
            DateTimeOffset.UtcNow,
            userId,
            requestData.RemoteIpAddress,
            requestData.UserAgent,
            apiKey);

        usageRecord.RecordResponse(
            requestData.StatusCode,
            requestData.ResponseTimeMs,
            requestData.RequestSize,
            requestData.ResponseSize);

        // Check if this is a FIRS operation
        if (requestData.Path.Contains("/firs", StringComparison.OrdinalIgnoreCase))
        {
            var operation = DetermineFIRSOperation(requestData.Path, requestData.Method);
            var invoiceId = ExtractInvoiceIdFromPath(requestData.Path);
            var usedAegisCredentials = _configuration.GetValue<bool>("FIRSConfiguration:UseAegisCredentials");
            
            usageRecord.RecordFIRSOperation(invoiceId, usedAegisCredentials);
        }

        dbContext.ApiUsageTrackings.Add(usageRecord);
        await dbContext.SaveChangesAsync();

        _logger.LogInformation(
            "API usage recorded: {Method} {Path} - {StatusCode} in {ResponseTime}ms",
            requestData.Method,
            requestData.Path,
            requestData.StatusCode,
            requestData.ResponseTimeMs);
    }

    private Guid? GetUserIdFromClaims(ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private Guid? GetBusinessIdFromClaims(ClaimsPrincipal user)
    {
        var businessIdClaim = user.FindFirst("BusinessId")?.Value;
        if (!string.IsNullOrEmpty(businessIdClaim) && Guid.TryParse(businessIdClaim, out var businessId))
        {
            return businessId;
        }
        return null;
    }

    private string? GetApiKeyFromRequest(HttpContext context)
    {
        // Check header first
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKey))
        {
            return apiKey.ToString();
        }

        // Check query string
        if (context.Request.Query.TryGetValue("api_key", out var queryApiKey))
        {
            return queryApiKey.ToString();
        }

        return null;
    }

    private async Task<Guid?> GetBusinessIdFromApiKeyAsync(IApplicationDbContext dbContext, string apiKey)
    {
        // Look up business by API key
        var business = await dbContext.Businesses
            .FirstOrDefaultAsync(b => b.ApiKey == apiKey && b.IsApiKeyActive);

        if (business != null)
        {
            // Record API key usage
            business.RecordApiKeyUsage();
            dbContext.Businesses.Update(business);
            await dbContext.SaveChangesAsync();
            
            return business.Id;
        }

        return null;
    }

    private string DetermineFIRSOperation(string path, string method)
    {
        if (path.Contains("validate", StringComparison.OrdinalIgnoreCase))
            return "ValidateInvoice";
        if (path.Contains("submit", StringComparison.OrdinalIgnoreCase))
            return "SubmitInvoice";
        if (path.Contains("report", StringComparison.OrdinalIgnoreCase))
            return "ReportInvoice";
        if (path.Contains("download", StringComparison.OrdinalIgnoreCase))
            return "DownloadInvoice";
        if (path.Contains("sign", StringComparison.OrdinalIgnoreCase))
            return "SignInvoice";
        if (path.Contains("authenticate", StringComparison.OrdinalIgnoreCase))
            return "Authenticate";
        
        return $"{method}_{path}";
    }

    private string? ExtractInvoiceIdFromPath(string path)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        // Look for invoice ID pattern (usually after "invoice" segment)
        for (int i = 0; i < segments.Length - 1; i++)
        {
            if (segments[i].Equals("invoice", StringComparison.OrdinalIgnoreCase) ||
                segments[i].Equals("invoices", StringComparison.OrdinalIgnoreCase))
            {
                return segments[i + 1];
            }
        }

        return null;
    }
}