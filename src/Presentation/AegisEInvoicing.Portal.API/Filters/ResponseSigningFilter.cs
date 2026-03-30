using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace AegisEInvoicing.Portal.API.Filters;

/// <summary>
/// Filter that automatically signs all API responses to prevent tampering.
/// Applies HMAC-SHA512 digital signature to critical responses.
/// </summary>
public sealed class ResponseSigningFilter : IAsyncResultFilter
{
    private readonly IResponseIntegrityService _integrityService;
    private readonly ILogger<ResponseSigningFilter> _logger;
    private readonly IConfiguration _configuration;

    public ResponseSigningFilter(
        IResponseIntegrityService integrityService,
        ILogger<ResponseSigningFilter> logger,
        IConfiguration configuration)
    {
        _integrityService = integrityService ?? throw new ArgumentNullException(nameof(integrityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        // Check if response signing is enabled
        var isSigningEnabled = _configuration.GetValue<bool>("ResponseIntegrity:EnableSigning", true);

        if (!isSigningEnabled)
        {
            await next();
            return;
        }

        // Only sign ObjectResult responses (API responses)
        if (context.Result is ObjectResult objectResult && objectResult.Value != null)
        {
            // Check if it's an ApiResponse that needs signing
            var valueType = objectResult.Value.GetType();
            if (valueType.IsGenericType && valueType.GetGenericTypeDefinition() == typeof(ApiResponse<>))
            {
                try
                {
                    await SignResponseAsync(context, objectResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error signing API response. Response will be sent unsigned.");
                    // Don't block the response if signing fails - log and continue
                }
            }
        }

        await next();
    }

    private async Task SignResponseAsync(ResultExecutingContext context, ObjectResult objectResult)
    {
        var response = objectResult.Value;
        if (response == null) return;

        var responseType = response.GetType();

        // Get properties using reflection
        var timestampProp = responseType.GetProperty("Timestamp");
        var traceIdProp = responseType.GetProperty("TraceId");
        var signatureProp = responseType.GetProperty("Signature");
        var requestIdProp = responseType.GetProperty("RequestId");
        var serverIdentityProp = responseType.GetProperty("ServerIdentity");
        var successProp = responseType.GetProperty("Success");

        if (timestampProp == null || signatureProp == null)
        {
            _logger.LogWarning("Response type does not have required properties for signing");
            return;
        }

        // Only sign successful responses or critical error responses
        var isSuccess = successProp?.GetValue(response) as bool? ?? false;
        var statusCode = objectResult.StatusCode ?? 200;

        // Sign all invoice-related operations and critical errors
        var shouldSign = isSuccess ||
                        statusCode == 400 ||
                        statusCode == 401 ||
                        statusCode == 403 ||
                        IsCriticalEndpoint(context);

        if (!shouldSign)
        {
            return;
        }

        // Get or set request ID
        var requestId = context.HttpContext.TraceIdentifier;
        if (traceIdProp != null && string.IsNullOrEmpty(traceIdProp.GetValue(response) as string))
        {
            traceIdProp.SetValue(response, requestId);
        }

        if (requestIdProp != null)
        {
            requestIdProp.SetValue(response, requestId);
        }

        // Set server identity
        if (serverIdentityProp != null)
        {
            serverIdentityProp.SetValue(response, Environment.MachineName);
        }

        // Get timestamp
        var timestamp = (DateTime)(timestampProp.GetValue(response) ?? DateTime.UtcNow);

        // Serialize response data for signing (exclude Signature field)
        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        // Temporarily set signature to null for serialization
        signatureProp.SetValue(response, null);

        var responseJson = JsonSerializer.Serialize(response, responseType, serializeOptions);

        // Generate signature
        var signature = await _integrityService.GenerateResponseSignatureAsync(
            responseJson,
            timestamp,
            requestId);

        // Set the signature
        signatureProp.SetValue(response, signature);

        _logger.LogDebug(
            "Signed API response for RequestId: {RequestId}, Endpoint: {Endpoint}",
            requestId,
            context.HttpContext.Request.Path);
    }

    private bool IsCriticalEndpoint(ResultExecutingContext context)
    {
        var path = context.HttpContext.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Define critical endpoints that must always be signed
        var criticalPaths = new[]
        {
            "/api/invoice/validate",
            "/api/invoice/sign",
            "/api/invoice/transmit",
            "/api/invoice/approve",
            "/api/invoice/reject",
            "/api/authentication/login",
            "/api/authentication/refresh",
            "/api/business/",
            "/api/party/"
        };

        return criticalPaths.Any(cp => path.Contains(cp));
    }
}
