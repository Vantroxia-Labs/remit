using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using AegisEInvoicing.Application.Services.Authentication;
using AegisEInvoicing.ERP.API.Models;

namespace AegisEInvoicing.ERP.API.Authentication;

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
    public const string DefaultScheme = "ApiKey";
    public string Scheme => DefaultScheme;
    public string HeaderName { get; set; } = "X-API-Key";
    public string QueryStringKey { get; set; } = "api_key";
}

public class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IApiKeyAuthenticationService apiKeyService) : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, logger, encoder)
{
    private readonly IApiKeyAuthenticationService _apiKeyService = apiKeyService;
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger = logger.CreateLogger<ApiKeyAuthenticationHandler>();
    
    // Store validation error for use in challenge
    private string? _validationError;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        _logger.LogInformation("API Key authentication started for request: {Method} {Path}", Request.Method, Request.Path);

        // Try to get API key from header
        string? apiKey = null;

        if (Request.Headers.TryGetValue(Options.HeaderName, out var headerValues))
        {
            apiKey = headerValues.FirstOrDefault();
            _logger.LogInformation("API key found in header: {ApiKeyPrefix}***", apiKey?.Substring(0, Math.Min(10, apiKey.Length)));
        }

        // If not in header, try query string
        if (string.IsNullOrEmpty(apiKey) && Request.Query.TryGetValue(Options.QueryStringKey, out var queryValues))
        {
            apiKey = queryValues.FirstOrDefault();
            _logger.LogInformation("API key found in query string: {ApiKeyPrefix}***", apiKey?.Substring(0, Math.Min(10, apiKey.Length)));
        }

        // If no API key found, store error and return no result
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("No API key found in request headers or query parameters");
            _validationError = $"Missing required header '{Options.HeaderName}'. Please provide your API key in the {Options.HeaderName} header.";
            return AuthenticateResult.NoResult();
        }

        _logger.LogInformation("Validating API key: {ApiKeyPrefix}***", apiKey.Substring(0, Math.Min(10, apiKey.Length)));

        // Validate the API key
        var validationResult = await _apiKeyService.ValidateApiKeyAsync(apiKey);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("API key validation failed: {Error}", validationResult.Error);
            
            // Store detailed error for challenge response
            _validationError = validationResult.Error switch
            {
                "API key is required" => $"Missing required header '{Options.HeaderName}'. Please provide your API key.",
                "Invalid API key" => "Invalid API key. Please check your API key and try again. If you don't have an API key, please contact support to obtain one.",
                "Business has no active admin user" => "Your business account has no active admin user. Please contact support to resolve this issue.",
                "Business account is not active" => "Your business account is not active. Please contact your administrator or support.",
                "Subscription is not active or has expired" => "Your subscription is not active or has expired. Please renew your subscription to continue using the API.",
                "Subscription plan information is not available" => "Your subscription plan information is not available. Please contact support.",
                not null when validationResult.Error.Contains("does not include API access") => 
                    "Your subscription plan does not include API access. Please upgrade to API-Only or SaaS plan to use the API.",
                not null => $"API key validation failed: {validationResult.Error}",
                _ => "API key validation failed. Please contact support."
            };
            
            return AuthenticateResult.Fail(_validationError);
        }

        _logger.LogInformation("API key validation successful for business: {BusinessId}", validationResult.BusinessId);

        // Create the authenticated user
        var identity = new ClaimsIdentity(validationResult.Claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        // Store rate limit tier in HttpContext for rate limiting middleware
        Context.Items["RateLimitTier"] = validationResult.RateLimitTier;
        Context.Items["BusinessId"] = validationResult.BusinessId;

        return AuthenticateResult.Success(ticket);
    }

    protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/json";
        
        // Use stored validation error or default message
        var errorMessage = _validationError ?? $"Missing required header '{Options.HeaderName}'. Please provide your API key in the {Options.HeaderName} header.";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = errorMessage,
            TraceId = Context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await Response.WriteAsync(json);
    }

    protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 403;
        Response.ContentType = "application/json";
        
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "Access forbidden. Your API key does not have permission to access this resource. Please contact your administrator if you believe this is an error.",
            TraceId = Context.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        await Response.WriteAsync(json);
    }
}
