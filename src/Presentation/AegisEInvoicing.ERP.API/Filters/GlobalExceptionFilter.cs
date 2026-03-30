using AegisEInvoicing.ERP.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;

namespace AegisEInvoicing.ERP.API.Filters;

/// <summary>
/// Global exception filter for controllers
/// </summary>
public sealed class GlobalExceptionFilter(
    ILogger<GlobalExceptionFilter> logger,
    IWebHostEnvironment environment) : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger = logger;
    private readonly IWebHostEnvironment _environment = environment;

    public void OnException(ExceptionContext context)
    {
        // Generate unique error ID for correlation (VAPT finding: Application Error Disclosure)
        var errorId = Guid.NewGuid().ToString("N")[..8];

        _logger.LogError(context.Exception, "Unhandled exception in {Controller}.{Action}. ErrorId: {ErrorId}",
            context.ActionDescriptor.RouteValues["controller"],
            context.ActionDescriptor.RouteValues["action"],
            errorId);

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = "An internal error occurred. Please contact support if the problem persists.",
            TraceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
        };

        // Only include developer details in Development environment (never in Production)
        // VAPT: Removed exception.ToString() to prevent sensitive information disclosure
        if (_environment.IsDevelopment())
        {
            response.DeveloperMessage = $"ErrorId: {errorId}. Check server logs for details.";
        }

        context.Result = new ObjectResult(response)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}