namespace AegisEInvoicing.SFTP.API.Middleware;

/// <summary>
/// Middleware to enforce HTTPS connections and block unencrypted HTTP requests
/// Addresses VAPT finding: CSP Header not set / Unencrypted communications
/// </summary>
public class HttpsEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<HttpsEnforcementMiddleware> _logger;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public HttpsEnforcementMiddleware(
        RequestDelegate next,
        ILogger<HttpsEnforcementMiddleware> logger,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if HTTPS enforcement is enabled (default: true in production)
        var enforceHttps = _configuration.GetValue<bool>(
            "Security:EnforceHttps",
            _environment.IsProduction());

        var isHttps = context.Request.IsHttps;

        if (enforceHttps && !isHttps)
        {
            var httpsPort = _configuration.GetValue<int>("Security:HttpsPort", 443);
            var host = context.Request.Host;

            var httpsHost = httpsPort == 443
                ? new HostString(host.Host)
                : new HostString(host.Host, httpsPort);

            var httpsUrl = $"https://{httpsHost}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";

            _logger.LogWarning(
                "HTTPS enforcement: Blocking unencrypted HTTP request from {IpAddress} to {Path}. Redirecting to HTTPS.",
                context.Connection.RemoteIpAddress,
                context.Request.Path);

            var blockHttp = _configuration.GetValue<bool>("Security:BlockHttpRequests", false);

            if (blockHttp)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                context.Response.Headers.Remove("Server");

                var errorResponse = new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.3",
                    title = "HTTPS Required",
                    status = 403,
                    detail = "This service requires HTTPS. Unencrypted HTTP connections are not permitted.",
                    traceId = context.TraceIdentifier
                };

                await context.Response.WriteAsJsonAsync(errorResponse);
                return;
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = httpsUrl;
                context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
                return;
            }
        }

        await _next(context);
    }
}

/// <summary>
/// Extension methods for registering HTTPS enforcement middleware
/// </summary>
public static class HttpsEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UseHttpsEnforcement(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<HttpsEnforcementMiddleware>();
    }
}
