namespace AegisEInvoicing.SFTP.API.Middleware;

/// <summary>
/// Middleware to add comprehensive security headers to all HTTP responses
/// Addresses VAPT finding: Security headers misconfiguration
/// Protects against: XSS, Clickjacking, MIME sniffing, Protocol downgrade, Data injection
/// </summary>
public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityHeadersMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(
        RequestDelegate next,
        ILogger<SecurityHeadersMiddleware> logger,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Register callback to modify headers just before response is sent
        // This ensures we can remove headers that are added by the server
        context.Response.OnStarting(() =>
        {
            AddSecurityHeaders(context);
            RemoveServerHeaders(context);
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private void AddSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // =================================================================
        // CRITICAL SECURITY HEADERS (VAPT Requirements)
        // =================================================================

        // 1. X-Content-Type-Options: Prevents MIME type sniffing
        // Protects against drive-by download attacks
        headers["X-Content-Type-Options"] = GetConfigValue("SecurityHeaders:XContentTypeOptions", "nosniff");

        // 2. X-Frame-Options: Prevents clickjacking attacks
        // DENY: Prevents page from being embedded in iframes on ANY site
        headers["X-Frame-Options"] = GetConfigValue("SecurityHeaders:XFrameOptions", "DENY");

        // 3. Referrer-Policy: Controls referrer information leakage
        headers["Referrer-Policy"] = GetConfigValue("SecurityHeaders:ReferrerPolicy", "strict-origin-when-cross-origin");

        // 4. Permissions-Policy (formerly Feature-Policy): Controls browser features
        var permissionsPolicy = GetConfigValue("SecurityHeaders:PermissionsPolicy",
            "geolocation=(), camera=(), microphone=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()");
        headers["Permissions-Policy"] = permissionsPolicy;

        // 5. Content-Security-Policy: THE MOST IMPORTANT XSS/injection protection header
        // BackgroundService always uses strict CSP (no Swagger in production)
        headers["Content-Security-Policy"] = BuildStrictCsp();

        // 6. Strict-Transport-Security (HSTS): Forces HTTPS connections
        var hstsMaxAge = GetConfigValue("SecurityHeaders:HSTS:MaxAge", "31536000");
        var hstsIncludeSubdomains = GetConfigValue("SecurityHeaders:HSTS:IncludeSubDomains", "true").ToLower() == "true";
        var hstsPreload = GetConfigValue("SecurityHeaders:HSTS:Preload", "true").ToLower() == "true";

        var hstsValue = $"max-age={hstsMaxAge}";
        if (hstsIncludeSubdomains) hstsValue += "; includeSubDomains";
        if (hstsPreload) hstsValue += "; preload";

        headers["Strict-Transport-Security"] = hstsValue;

        // =================================================================
        // ADDITIONAL SECURITY HEADERS
        // =================================================================

        // 7. X-Permitted-Cross-Domain-Policies: Restricts Adobe Flash/PDF cross-domain policies
        headers["X-Permitted-Cross-Domain-Policies"] = GetConfigValue("SecurityHeaders:XPermittedCrossDomainPolicies", "none");

        // 8. Cross-Origin-Opener-Policy (COOP): Prevents cross-origin attacks
        // BackgroundService uses strict same-origin (no client-side CORS needed)
        headers["Cross-Origin-Opener-Policy"] = GetConfigValue("SecurityHeaders:CrossOriginOpenerPolicy", "same-origin");

        // 9. Cross-Origin-Resource-Policy (CORP): Prevents cross-origin resource leaks
        headers["Cross-Origin-Resource-Policy"] = GetConfigValue("SecurityHeaders:CrossOriginResourcePolicy", "same-origin");

        // 10. Cross-Origin-Embedder-Policy (COEP): Requires explicit permission for cross-origin resources
        headers["Cross-Origin-Embedder-Policy"] = GetConfigValue("SecurityHeaders:CrossOriginEmbedderPolicy", "require-corp");

        // 11. X-DNS-Prefetch-Control: Controls DNS prefetching
        headers["X-DNS-Prefetch-Control"] = GetConfigValue("SecurityHeaders:XDNSPrefetchControl", "off");

        // =================================================================
        // CACHE CONTROL FOR API RESPONSES
        // =================================================================
        // Addresses VAPT finding: Cacheable HTTP Response
        ApplyCacheControlHeaders(context);

        _logger.LogDebug("Applied security headers to response for {Path}", context.Request.Path);
    }

    private void ApplyCacheControlHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Health check endpoints can be cached briefly
        if (path.StartsWith("/health"))
        {
            headers["Cache-Control"] = "public, max-age=5";
        }
        // All other endpoints - no caching for security
        else
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }
    }

    private string GetConfigValue(string key, string defaultValue)
    {
        return _configuration.GetValue<string>(key) ?? defaultValue;
    }

    private static string BuildStrictCsp()
    {
        // Strict CSP for BackgroundService - Maximum security
        // VAPT: No wildcards, no unsafe-inline, no unsafe-eval
        var cspDirectives = new[]
        {
            // default-src: Fallback for all fetch directives
            "default-src 'self'",

            // script-src: JavaScript sources (NO unsafe-inline or unsafe-eval)
            "script-src 'self'",

            // style-src: CSS sources (NO unsafe-inline)
            "style-src 'self'",

            // img-src: Image sources
            "img-src 'self' data:",

            // font-src: Font sources
            "font-src 'self' data:",

            // connect-src: XHR, WebSocket, EventSource sources
            "connect-src 'self'",

            // frame-src: iframe sources
            "frame-src 'none'",

            // frame-ancestors: Where this page can be embedded
            "frame-ancestors 'none'",

            // base-uri: Restricts <base> tag
            "base-uri 'self'",

            // form-action: Restricts form submission targets
            "form-action 'self'",

            // object-src: Flash, Java applets, other plugins
            "object-src 'none'",

            // media-src: Audio and video sources
            "media-src 'self'",

            // manifest-src: Web app manifest sources
            "manifest-src 'self'",

            // worker-src: Web worker sources
            "worker-src 'self'",

            // child-src: Web workers and nested browsing contexts
            "child-src 'none'",

            // upgrade-insecure-requests: Automatically upgrade HTTP to HTTPS
            "upgrade-insecure-requests",

            // block-all-mixed-content: Block all HTTP content on HTTPS pages
            "block-all-mixed-content"
        };

        return string.Join("; ", cspDirectives);
    }

    private void RemoveServerHeaders(HttpContext context)
    {
        // =================================================================
        // REMOVE SERVER IDENTIFICATION HEADERS
        // Addresses VAPT finding: Information Disclosure through HTTP Response Headers
        // =================================================================

        // Remove IIS server headers
        context.Response.Headers.Remove("Server");

        // Remove ASP.NET identification headers
        context.Response.Headers.Remove("X-Powered-By");
        context.Response.Headers.Remove("X-AspNet-Version");
        context.Response.Headers.Remove("X-AspNetMvc-Version");
        context.Response.Headers.Remove("X-AspNetCore-Version");

        // Remove Kestrel server header
        context.Response.Headers.Remove("Kestrel-Version");

        // Remove deprecated X-XSS-Protection header if present
        context.Response.Headers.Remove("X-XSS-Protection");

        _logger.LogDebug("Removed server identification headers from response");
    }
}

/// <summary>
/// Extension methods for registering security headers middleware
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
