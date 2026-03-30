using AegisEInvoicing.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace AegisEInvoicing.ERP.API.Middleware;

/// <summary>
/// Global exception handler that prevents sensitive information disclosure in error responses
/// Addresses VAPT finding: Improper Error Disclosure
/// Ensures generic error messages are returned to clients while detailed errors are logged
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // =================================================================
        // SECURITY: Generate unique error ID for correlation
        // This allows support teams to find detailed logs without exposing
        // sensitive information to end users
        // =================================================================
        var errorId = Guid.NewGuid().ToString();

        // =================================================================
        // SECURITY: Log full exception details with error ID
        // Detailed information is logged but NEVER sent to client
        // =================================================================
        LogException(exception, errorId, httpContext);

        // =================================================================
        // SECURITY: Create safe problem details for client response
        // Different handling for Development vs Production environments
        // =================================================================
        var problemDetails = CreateSafeProblemDetails(exception, errorId, httpContext);

        // Set response details
        httpContext.Response.StatusCode = problemDetails.Status ?? 500;
        httpContext.Response.ContentType = "application/problem+json";

        // Remove any headers that might disclose server information
        httpContext.Response.Headers.Remove("Server");
        httpContext.Response.Headers.Remove("X-Powered-By");

        // Write the problem details to the response
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    /// <summary>
    /// Logs exception with full details including stack trace, inner exceptions, and context
    /// This is for internal logging only - NEVER expose these details to clients
    /// </summary>
    private void LogException(Exception exception, string errorId, HttpContext httpContext)
    {
        var requestPath = httpContext.Request.Path;
        var requestMethod = httpContext.Request.Method;
        var userIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var userId = httpContext.User?.Identity?.Name ?? "Anonymous";

        // Log with ERROR level for all exceptions
        _logger.LogError(
            exception,
            "EXCEPTION [{ErrorId}] - Type: {ExceptionType}, Message: {Message}, " +
            "Path: {Path}, Method: {Method}, User: {User}, IP: {IP}",
            errorId,
            exception.GetType().Name,
            exception.Message,
            requestPath,
            requestMethod,
            userId,
            userIp);

        // Log inner exceptions if present
        if (exception.InnerException != null)
        {
            _logger.LogError(
                exception.InnerException,
                "INNER EXCEPTION [{ErrorId}] - Type: {ExceptionType}, Message: {Message}",
                errorId,
                exception.InnerException.GetType().Name,
                exception.InnerException.Message);
        }
    }

    /// <summary>
    /// Creates safe problem details that prevent information disclosure
    /// Development: Shows more details for debugging
    /// Production: Shows only generic messages to prevent leaking sensitive information
    /// </summary>
    private ProblemDetails CreateSafeProblemDetails(Exception exception, string errorId, HttpContext httpContext)
    {
        return exception switch
        {
            // Validation exceptions - safe to show validation errors to users
            ValidationException validationException => CreateValidationProblemDetails(validationException, errorId),

            // Application exceptions - controlled messages designed for end users
            AppException appException => CreateAppExceptionProblemDetails(appException, errorId),

            // Authorization - generic message only
            UnauthorizedAccessException => CreateProblemDetails(
                "Unauthorized",
                HttpStatusCode.Unauthorized,
                "You do not have permission to access this resource.",
                "UNAUTHORIZED",
                errorId),

            // Argument/validation from code - sanitize in production
            ArgumentException argumentException => CreateSanitizedProblemDetails(
                "Bad Request",
                HttpStatusCode.BadRequest,
                argumentException.Message,
                "The request contains invalid data.",
                "INVALID_REQUEST",
                errorId),

            // Invalid operation - sanitize in production
            InvalidOperationException invalidOperationException => CreateSanitizedProblemDetails(
                "Bad Request",
                HttpStatusCode.BadRequest,
                invalidOperationException.Message,
                "The requested operation cannot be completed.",
                "INVALID_OPERATION",
                errorId),

            // Database/data access exceptions - NEVER expose database details
            Microsoft.EntityFrameworkCore.DbUpdateException => CreateProblemDetails(
                "Internal Server Error",
                HttpStatusCode.InternalServerError,
                "An error occurred while processing your request. Please try again later.",
                "DATABASE_ERROR",
                errorId),

            // Null reference - NEVER expose code details
            NullReferenceException => CreateProblemDetails(
                "Internal Server Error",
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please contact support if this persists.",
                "INTERNAL_ERROR",
                errorId),

            // Timeout exceptions
            TimeoutException => CreateProblemDetails(
                "Request Timeout",
                HttpStatusCode.RequestTimeout,
                "The request took too long to process. Please try again.",
                "TIMEOUT",
                errorId),

            // Task cancelled
            TaskCanceledException or OperationCanceledException => CreateProblemDetails(
                "Request Timeout",
                HttpStatusCode.RequestTimeout,
                "The request was cancelled or timed out.",
                "CANCELLED",
                errorId),

            // Default - completely generic message for any unexpected exception
            _ => CreateProblemDetails(
                "Internal Server Error",
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred. Please contact support with error ID: " + errorId,
                "INTERNAL_ERROR",
                errorId)
        };
    }

    /// <summary>
    /// Creates sanitized problem details - shows details in Development, generic in Production
    /// </summary>
    private ProblemDetails CreateSanitizedProblemDetails(
        string title,
        HttpStatusCode statusCode,
        string developmentMessage,
        string productionMessage,
        string errorCode,
        string errorId)
    {
        // SECURITY: Only show detailed messages in Development environment
        // Production always gets generic messages to prevent information disclosure
        var safeMessage = _environment.IsDevelopment()
            ? developmentMessage
            : productionMessage;

        return CreateProblemDetails(title, statusCode, safeMessage, errorCode, errorId);
    }

    /// <summary>
    /// Creates validation problem details with sanitized validation errors
    /// Validation errors are generally safe to expose as they're designed for user feedback
    /// </summary>
    private static ProblemDetails CreateValidationProblemDetails(ValidationException validationException, string errorId)
    {
        var problemDetails = new ValidationProblemDetails
        {
            Title = "Validation Error",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "One or more validation errors occurred. Please check your input and try again.",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Extensions =
            {
                ["errorCode"] = "VALIDATION_FAILED",
                ["errorId"] = errorId
            }
        };

        // Group validation errors by property name
        // These are safe to expose as they're designed for user feedback
        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        foreach (var error in errors)
        {
            problemDetails.Errors.Add(error.Key, error.Value);
        }

        return problemDetails;
    }

    /// <summary>
    /// Creates problem details for AppException (application-defined exceptions)
    /// AppException messages are designed for end users, so they're safe to expose
    /// </summary>
    private static ProblemDetails CreateAppExceptionProblemDetails(AppException appException, string errorId)
    {
        var problemDetails = new ProblemDetails
        {
            Title = GetTitleForStatusCode(appException.StatusCode),
            Status = (int)appException.StatusCode,
            Detail = appException.Message, // Safe: AppException messages are designed for users
            Type = GetTypeForStatusCode(appException.StatusCode),
            Extensions =
            {
                ["errorCode"] = appException.ErrorCode,
                ["errorId"] = errorId
            }
        };

        // Only include details if they don't contain sensitive information
        // AppException.Details should be sanitized at the source
        if (appException.Details != null)
        {
            problemDetails.Extensions["details"] = appException.Details;
        }

        return problemDetails;
    }

    /// <summary>
    /// Creates generic problem details with error ID for correlation
    /// </summary>
    private static ProblemDetails CreateProblemDetails(
        string title,
        HttpStatusCode statusCode,
        string detail,
        string errorCode,
        string errorId)
    {
        return new ProblemDetails
        {
            Title = title,
            Status = (int)statusCode,
            Detail = detail,
            Type = GetTypeForStatusCode(statusCode),
            Extensions =
            {
                ["errorCode"] = errorCode,
                ["errorId"] = errorId,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        };
    }

    private static string GetTitleForStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.Forbidden => "Forbidden",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.Conflict => "Conflict",
            HttpStatusCode.UnprocessableEntity => "Unprocessable Entity",
            HttpStatusCode.TooManyRequests => "Too Many Requests",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            HttpStatusCode.ServiceUnavailable => "Service Unavailable",
            HttpStatusCode.RequestTimeout => "Request Timeout",
            HttpStatusCode.NotImplemented => "Not Implemented",
            _ => "Error"
        };
    }

    private static string GetTypeForStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
            HttpStatusCode.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            HttpStatusCode.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            HttpStatusCode.UnprocessableEntity => "https://tools.ietf.org/html/rfc4918#section-11.2",
            HttpStatusCode.TooManyRequests => "https://tools.ietf.org/html/rfc6585#section-4",
            HttpStatusCode.InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            HttpStatusCode.ServiceUnavailable => "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            HttpStatusCode.RequestTimeout => "https://tools.ietf.org/html/rfc7231#section-6.5.7",
            HttpStatusCode.NotImplemented => "https://tools.ietf.org/html/rfc7231#section-6.6.2",
            _ => "https://tools.ietf.org/html/rfc7231"
        };
    }
}
