using Microsoft.Extensions.Primitives;

namespace AegisEInvoicing.ERP.API.Middleware;

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
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // Skip ALL security headers for API documentation paths (Scalar/OpenAPI)
        // Scalar requires inline scripts, styles, and CDN resources to function
        // This includes the main scalar page and all its assets
        if (path.StartsWith("/scalar") || path.StartsWith("/openapi") || path.Contains("/scalar/"))
        {
            // Still remove server identification headers for minimal security
            context.Response.OnStarting(() =>
            {
                RemoveServerHeaders(context);
                return Task.CompletedTask;
            });

            await _next(context);
            return;
        }

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
        // Protects against drive-by download attacks and reduces exposure to user-uploaded content risks
        headers["X-Content-Type-Options"] = GetConfigValue("SecurityHeaders:XContentTypeOptions", "nosniff");

        // 2. X-Frame-Options: Prevents clickjacking attacks
        // DENY: Prevents page from being embedded in iframes on ANY site (most secure)
        // SAMEORIGIN: Allows embedding only on same origin
        headers["X-Frame-Options"] = GetConfigValue("SecurityHeaders:XFrameOptions", "DENY");

        // 3. Referrer-Policy: Controls referrer information leakage
        // strict-origin-when-cross-origin: Balances privacy and functionality
        // no-referrer: Maximum privacy (may break some analytics)
        // same-origin: Only send referrer to same origin
        headers["Referrer-Policy"] = GetConfigValue("SecurityHeaders:ReferrerPolicy", "strict-origin-when-cross-origin");

        // 4. Permissions-Policy (formerly Feature-Policy): Controls browser features
        // Restricts access to sensitive features: geolocation, camera, microphone, payment, etc.
        var permissionsPolicy = GetConfigValue("SecurityHeaders:PermissionsPolicy",
            "geolocation=(), camera=(), microphone=(), payment=(), usb=(), magnetometer=(), gyroscope=(), accelerometer=()");
        headers["Permissions-Policy"] = permissionsPolicy;

        // 5. Content-Security-Policy: THE MOST IMPORTANT XSS/injection protection header
        // Defines trusted sources for scripts, styles, images, etc.
        // Different policies for production (strict) vs development (relaxed for Swagger)
        if (_environment.IsProduction())
        {
            headers["Content-Security-Policy"] = BuildProductionCsp();
            _logger.LogInformation("Applied production Content-Security-Policy");
        }
        else if (_environment.IsDevelopment())
        {
            headers["Content-Security-Policy"] = BuildDevelopmentCsp();
            _logger.LogDebug("Applied development Content-Security-Policy (relaxed for Swagger)");
        }

        // 6. Strict-Transport-Security (HSTS): Forces HTTPS connections
        // Critical for preventing protocol downgrade attacks and cookie hijacking
        // max-age: Duration to remember HTTPS-only rule (1 year = 31536000 seconds)
        // includeSubDomains: Apply to all subdomains
        // preload: Allow inclusion in browser HSTS preload lists (hstspreload.org)
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
        // Prevents Adobe products from loading cross-domain content
        headers["X-Permitted-Cross-Domain-Policies"] = GetConfigValue("SecurityHeaders:XPermittedCrossDomainPolicies", "none");

        // 8. Cross-Origin-Opener-Policy (COOP): Prevents cross-origin attacks
        // same-origin-allow-popups: Allows CORS requests while maintaining some isolation
        // Note: Changed from "same-origin" to allow CORS API requests from trusted origins
        headers["Cross-Origin-Opener-Policy"] = GetConfigValue("SecurityHeaders:CrossOriginOpenerPolicy", "same-origin-allow-popups");

        // 9. Cross-Origin-Resource-Policy (CORP): Prevents cross-origin resource leaks
        // cross-origin: Allow cross-origin requests (controlled by CORS policy)
        // Note: Changed from "same-origin" to allow CORS API requests from trusted origins
        // Security: CORS middleware already controls which origins can access the API
        headers["Cross-Origin-Resource-Policy"] = GetConfigValue("SecurityHeaders:CrossOriginResourcePolicy", "cross-origin");

        // 10. Cross-Origin-Embedder-Policy (COEP): Requires explicit permission for cross-origin resources
        // unsafe-none: Disabled to allow CORS requests
        // Note: Changed from "require-corp" to allow CORS API requests from trusted origins
        // Security: CORS middleware already controls which origins can access the API
        headers["Cross-Origin-Embedder-Policy"] = GetConfigValue("SecurityHeaders:CrossOriginEmbedderPolicy", "unsafe-none");

        // 11. X-DNS-Prefetch-Control: Controls DNS prefetching
        // off: Disables DNS prefetching to enhance privacy
        headers["X-DNS-Prefetch-Control"] = GetConfigValue("SecurityHeaders:XDNSPrefetchControl", "off");

        // =================================================================
        // CACHE CONTROL FOR SENSITIVE API RESPONSES
        // =================================================================
        // Addresses VAPT finding: Cacheable HTTP Response
        // Prevents sensitive data from being cached in browser/proxy caches

        ApplyCacheControlHeaders(context);

        _logger.LogDebug("Applied {Count} security headers to response for {Path}",
            headers.Count, context.Request.Path);
    }

    private void ApplyCacheControlHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;
        var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

        // =================================================================
        // SENSITIVE ENDPOINTS - NEVER CACHE
        // =================================================================
        // These endpoints handle authentication, user data, sessions, and other sensitive information
        // Risk: Data leakage, session hijacking if cached
        if (IsSensitiveEndpoint(path))
        {
            // no-store: Prevents storing the response in any cache (browser, proxy, CDN)
            // no-cache: Requires revalidation with server before using cached copy
            // must-revalidate: Forces cache to check with server if stale
            // private: Response is for single user only, not shared caches
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";

            // Pragma: HTTP/1.0 backward compatibility (legacy support)
            headers["Pragma"] = "no-cache";

            // Expires: Set to epoch (Jan 1, 1970) to indicate already expired
            // This ensures old browsers/proxies don't cache
            headers["Expires"] = "0";

            _logger.LogDebug(
                "Applied no-cache headers to sensitive endpoint: {Path}",
                context.Request.Path);
        }
        // =================================================================
        // STATIC ASSETS - ALLOW CACHING
        // =================================================================
        // Static resources like Swagger UI, CSS, JS, images can be cached
        // These don't contain sensitive user data
        else if (IsStaticAsset(path))
        {
            // public: Can be cached by any cache (browser, proxy, CDN)
            // max-age=3600: Cache for 1 hour (3600 seconds)
            headers["Cache-Control"] = "public, max-age=3600";

            _logger.LogDebug(
                "Applied caching headers to static asset: {Path}",
                context.Request.Path);
        }
        // =================================================================
        // DEFAULT API RESPONSES - NO CACHE
        // =================================================================
        // All other API endpoints default to no-cache for security
        else if (path.StartsWith("/api"))
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, private";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";

            _logger.LogDebug(
                "Applied no-cache headers to API endpoint: {Path}",
                context.Request.Path);
        }
    }

    /// <summary>
    /// Determines if an endpoint handles sensitive data that should NEVER be cached
    /// </summary>
    private static bool IsSensitiveEndpoint(string path)
    {
        // Authentication and authorization endpoints
        if (path.Contains("/login") || path.Contains("/logout") ||
            path.Contains("/auth") || path.Contains("/token") ||
            path.Contains("/refresh-token") || path.Contains("/revoke-token"))
            return true;

        // User profile and personal data
        if (path.Contains("/user") || path.Contains("/profile") ||
            path.Contains("/account") || path.Contains("/me"))
            return true;

        // Password management
        if (path.Contains("/password") || path.Contains("/change-password") ||
            path.Contains("/reset-password") || path.Contains("/forgot-password"))
            return true;

        // Session management
        if (path.Contains("/session"))
            return true;

        // Business and invoice data (contains sensitive financial information)
        if (path.Contains("/business") || path.Contains("/invoice") ||
            path.Contains("/payment") || path.Contains("/transaction"))
            return true;

        // Settings and configuration (may contain API keys, credentials)
        if (path.Contains("/settings") || path.Contains("/config") ||
            path.Contains("/api-key"))
            return true;

        // Admin and management endpoints
        if (path.Contains("/admin") || path.Contains("/manage"))
            return true;

        return false;
    }

    /// <summary>
    /// Determines if a path is a static asset that can be safely cached
    /// </summary>
    private static bool IsStaticAsset(string path)
    {
        // Swagger UI and documentation (static HTML/JS/CSS)
        if (path.StartsWith("/swagger"))
            return true;

        // Health check endpoint (no sensitive data)
        if (path.StartsWith("/health"))
            return true;

        // Static file extensions
        var staticExtensions = new[] { ".css", ".js", ".jpg", ".jpeg", ".png", ".gif", ".svg", ".woff", ".woff2", ".ttf", ".eot", ".ico" };
        return staticExtensions.Any(ext => path.EndsWith(ext));
    }

    private string GetConfigValue(string key, string defaultValue)
    {
        return _configuration.GetValue<string>(key) ?? defaultValue;
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
        // This header is deprecated and can introduce vulnerabilities
        context.Response.Headers.Remove("X-XSS-Protection");

        _logger.LogDebug("Removed server identification headers from response");
    }

    private string BuildProductionCsp()
    {
        // Production CSP - Maximum security, no inline scripts/styles
        // All directives are explicitly defined following OWASP best practices
        // VAPT: Removed wildcard directives (https:, ws:, wss:) and unsafe-inline/unsafe-eval
        var cspDirectives = new[]
        {
            // default-src: Fallback for all fetch directives
            "default-src 'self'",

            // script-src: JavaScript sources (NO unsafe-inline or unsafe-eval in production)
            // VAPT: Ensures no inline scripts or eval() can execute
            "script-src 'self'",

            // style-src: CSS sources (NO unsafe-inline in production for maximum security)
            // VAPT: Removed unsafe-inline to prevent style injection attacks
            // Note: If styles break, consider using nonces or hashes instead
            "style-src 'self'",

            // img-src: Image sources (VAPT: Removed https: wildcard, only allow self and data URIs)
            // If specific CDNs are needed, add them explicitly (e.g., 'self' data: https://cdn.example.com)
            "img-src 'self' data:",

            // font-src: Font sources
            "font-src 'self' data:",

            // connect-src: XHR, WebSocket, EventSource sources (VAPT: Only allow self, no wildcards)
            "connect-src 'self'",

            // frame-src: iframe sources
            "frame-src 'none'",

            // frame-ancestors: Where this page can be embedded (DENY equivalent in CSP)
            "frame-ancestors 'none'",

            // base-uri: Restricts <base> tag to prevent base tag injection
            "base-uri 'self'",

            // form-action: Restricts form submission targets
            "form-action 'self'",

            // object-src: Flash, Java applets, other plugins (always none)
            "object-src 'none'",

            // media-src: Audio and video sources
            "media-src 'self'",

            // manifest-src: Web app manifest sources
            "manifest-src 'self'",

            // worker-src: Web worker sources
            "worker-src 'self'",

            // child-src: Web workers and nested browsing contexts (frames, iframes)
            "child-src 'none'",

            // upgrade-insecure-requests: Automatically upgrade HTTP to HTTPS
            "upgrade-insecure-requests",

            // block-all-mixed-content: Block all HTTP content on HTTPS pages
            "block-all-mixed-content"
        };

        return string.Join("; ", cspDirectives);
    }

    private string BuildDevelopmentCsp()
    {
        // Development CSP - Relaxed for Scalar UI and development tools
        // Scalar requires unsafe-inline and unsafe-eval for its JavaScript and CDN resources
        var cspDirectives = new[]
        {
            "default-src 'self'",

            // Scalar needs unsafe-inline and unsafe-eval for dynamic script generation
            // Also allow CDN for Scalar assets
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net",

            // Scalar needs unsafe-inline for styling and CDN fonts
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com",

            // Allow images from anywhere in development
            "img-src 'self' data: https:",

            // Font sources - include CDN for Scalar
            "font-src 'self' data: https://fonts.gstatic.com https://cdn.jsdelivr.net",

            // Allow WebSockets for hot reload in development
            "connect-src 'self' ws: wss:",

            // Allow iframes in development (for Scalar auth flows)
            "frame-src 'self'",

            // Still prevent embedding by other sites
            "frame-ancestors 'none'",

            // Restrict base URI
            "base-uri 'self'",

            // Allow form submissions to self
            "form-action 'self'",

            // Block plugins
            "object-src 'none'",

            // Media sources
            "media-src 'self'",

            // Allow workers in development (Scalar may use web workers)
            "worker-src 'self' blob:"
        };

        return string.Join("; ", cspDirectives);
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
