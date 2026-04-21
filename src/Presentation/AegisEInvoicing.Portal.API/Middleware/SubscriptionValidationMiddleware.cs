using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Infrastructure.Services;
using AegisEInvoicing.Infrastructure.Services.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Portal.API.Middleware;

public class SubscriptionValidationMiddleware(
    RequestDelegate next,
    ILogger<SubscriptionValidationMiddleware> logger,
    IServiceProvider serviceProvider)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<SubscriptionValidationMiddleware> _logger = logger;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly HashSet<string> _excludedPaths =
    [
        "/health",
        "/metrics",
        "/swagger",
        "/api/v1/system-setup",
        "/api/v2/system-setup",
        "/api/v1/authentication/login",
        "/api/v2/authentication/login"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip validation for excluded paths
        var path = context.Request.Path.Value?.ToLower() ?? string.Empty;
        if (_excludedPaths.Any(excluded => path.StartsWith(excluded)))
        {
            await _next(context);
            return;
        }

        // Skip for OPTIONS requests
        if (context.Request.Method == HttpMethods.Options)
        {
            await _next(context);
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var licenseService = scope.ServiceProvider.GetRequiredService<ILicenseValidationService>();
        var currentUserService = scope.ServiceProvider.GetService<ICurrentUserService>();

        try
        {
            // Get system configuration
            var systemConfig = await dbContext.SystemConfigurations
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (systemConfig == null)
            {
                // System not set up yet
                await _next(context);
                return;
            }

            // For On-Premise deployments, validate license
            if (systemConfig.DeploymentMode == DeploymentMode.OnPremise)
            {
                var isLicenseValid = await licenseService.ValidateLicenseAsync();
                
                if (!isLicenseValid)
                {
                    _logger.LogError("License validation failed for request to {Path}", path);
                    await WriteErrorResponse(context, 
                        "License validation failed. Please contact your administrator.", 
                        HttpStatusCode.PaymentRequired);
                    
                    // Shutdown application for critical paths
                    if (IsCriticalPath(path))
                    {
                        licenseService.ShutdownApplication("License validation failed on critical path");
                    }
                    
                    return;
                }
            }

            // For SaaS deployments, validate business subscription
            if (systemConfig.DeploymentMode == DeploymentMode.Cloud && currentUserService != null)
            {
                var userId = currentUserService.UserId;
                if (userId.HasValue)
                {
                    var user = await dbContext.Users
                        .Include(u => u.Business)
                        .ThenInclude(b => b!.Subscriptions)
                        .FirstOrDefaultAsync(u => u.Id == userId.Value);

                    if (user?.Business != null)
                    {
                        var subscription = user.Business.Subscriptions.FirstOrDefault(s => s.IsActive())
                                           ?? user.Business.Subscriptions.OrderByDescending(s => s.EndDate).FirstOrDefault();

                        if (subscription == null || !subscription.IsActive())
                        {
                            _logger.LogWarning("Subscription validation failed for user {UserId} on business {BusinessId}",
                                userId, user.Business.Id);

                            // Allow grace period for expired subscriptions
                            if (subscription?.IsGracePeriod() == true)
                            {
                                context.Response.Headers.Append("X-Subscription-Warning",
                                    $"Subscription expired. Grace period ends in {7 - subscription.DaysOverdue()} days");
                            }
                            else
                            {
                                await WriteErrorResponse(context, 
                                    "Your subscription has expired. Please contact support to renew.", 
                                    HttpStatusCode.PaymentRequired);
                                return;
                            }
                        }                      
                    }
                }
            }

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in subscription validation middleware");
            await _next(context); // Continue to avoid blocking all requests on error
        }
    }

    private static bool IsCriticalPath(string path)
    {
        var criticalPaths = new[] 
        { 
            "/invoice", 
            "/business", 
            "/firs",
            "/api/v1/invoice",
            "/api/v2/invoice"
        };
        
        return criticalPaths.Any(critical => path.Contains(critical, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task WriteErrorResponse(HttpContext context, string message, HttpStatusCode statusCode)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = new
            {
                message,
                code = statusCode.ToString(),
                timestamp = DateTimeOffset.UtcNow
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}