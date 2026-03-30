using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Diagnostics;
using System.Net;

namespace AegisEInvoicing.Portal.API.Filters;

/// <summary>
/// Global exception filter for controllers
/// </summary>
public sealed class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionFilter(
        ILogger<GlobalExceptionFilter> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;

        // Generate unique error ID for correlation (VAPT finding: Application Error Disclosure)
        var errorId = Guid.NewGuid().ToString("N")[..8];

        // Log the exception with appropriate level - include errorId for correlation
        LogException(exception, context, errorId);

        var (statusCode, message, errorCode, validationErrors) = GetErrorDetails(exception);

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            TraceId = traceId,
            ErrorCode = errorCode
        };

        // Add validation errors if present
        if (validationErrors?.Any() == true)
        {
            response.Errors = validationErrors;
        }

        // VAPT: Only include safe developer details in development environment
        // Never expose exception.ToString() as it may reveal sensitive stack traces and internal paths
        if (_environment.IsDevelopment())
        {
            response.DeveloperMessage = $"ErrorId: {errorId}. Check server logs for details.";
        }

        // Add additional details for custom exceptions
        if (exception is AppException appException && appException.Details != null)
        {
            response.Details = appException.Details;
        }

        context.Result = new ObjectResult(response)
        {
            StatusCode = (int)statusCode
        };

        context.ExceptionHandled = true;
    }

    private void LogException(Exception exception, ExceptionContext context, string errorId)
    {
        var controller = context.ActionDescriptor.RouteValues["controller"];
        var action = context.ActionDescriptor.RouteValues["action"];

        switch (exception)
        {
            case ValidationException:
                _logger.LogInformation(exception,
                    "Validation failed in {Controller}.{Action}: {Message}. ErrorId: {ErrorId}",
                    controller, action, exception.Message, errorId);
                break;

            case AppException appEx when appEx.StatusCode == HttpStatusCode.NotFound:
                _logger.LogWarning(exception,
                    "Resource not found in {Controller}.{Action}: {Message}. ErrorId: {ErrorId}",
                    controller, action, exception.Message, errorId);
                break;

            case AppException appEx when appEx.StatusCode < HttpStatusCode.InternalServerError:
                _logger.LogWarning(exception,
                    "Client error in {Controller}.{Action}: {Message}. ErrorId: {ErrorId}",
                    controller, action, exception.Message, errorId);
                break;

            case AppException appEx:
            default:
                _logger.LogError(exception,
                    "Unhandled exception in {Controller}.{Action}: {Message}. ErrorId: {ErrorId}",
                    controller, action, exception.Message, errorId);
                break;
        }
    }

    private (HttpStatusCode statusCode, string message, string errorCode, Dictionary<string, string[]>? validationErrors)
        GetErrorDetails(Exception exception)
    {
        return exception switch
        {
            // FluentValidation exceptions
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "One or more validation errors occurred.",
                "ValidationFailed",
                validationEx.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray()
                    )
            ),

            // Custom application exceptions
            BadRequestException badRequestEx => (
                HttpStatusCode.BadRequest,
                badRequestEx.Message,
                badRequestEx.ErrorCode,
                null
            ),

            AuthenticationException authEx => (
                HttpStatusCode.Unauthorized,
                authEx.Message,
                authEx.ErrorCode,
                null
            ),

            ForbiddenException forbiddenEx => (
                HttpStatusCode.Forbidden,
                forbiddenEx.Message,
                forbiddenEx.ErrorCode,
                null
            ),

            NotFoundException notFoundEx => (
                HttpStatusCode.NotFound,
                notFoundEx.Message,
                notFoundEx.ErrorCode,
                null
            ),

            ConflictException conflictEx => (
                HttpStatusCode.Conflict,
                conflictEx.Message,
                conflictEx.ErrorCode,
                null
            ),

            UnprocessableEntityException unprocessableEx => (
                HttpStatusCode.UnprocessableEntity,
                unprocessableEx.Message,
                unprocessableEx.ErrorCode,
                null
            ),

            TooManyRequestsException tooManyEx => (
                HttpStatusCode.TooManyRequests,
                tooManyEx.Message,
                tooManyEx.ErrorCode,
                null
            ),

            ServiceUnavailableException serviceUnavailableEx => (
                HttpStatusCode.ServiceUnavailable,
                serviceUnavailableEx.Message,
                serviceUnavailableEx.ErrorCode,
                null
            ),

            // Generic AppException (fallback for custom exceptions)
            AppException appEx => (
                appEx.StatusCode,
                appEx.Message,
                appEx.ErrorCode,
                null
            ),

            // Built-in .NET exceptions
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                argEx.Message,
                "InvalidArgument",
                null
            ),

            InvalidOperationException invalidOpEx => (
                HttpStatusCode.BadRequest,
                invalidOpEx.Message,
                "InvalidOperation",
                null
            ),

            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "Access denied",
                "Unauthorized",
                null
            ),

            FileNotFoundException fileNotFoundEx => (
                HttpStatusCode.NotFound,
                $"File not found: {fileNotFoundEx.FileName}",
                "FileNotFound",
                null
            ),

            DirectoryNotFoundException => (
                HttpStatusCode.NotFound,
                "Directory not found",
                "DirectoryNotFound",
                null
            ),

            TimeoutException timeoutEx => (
                HttpStatusCode.RequestTimeout,
                timeoutEx.Message,
                "Timeout",
                null
            ),

            NotSupportedException notSupportedEx => (
                HttpStatusCode.NotImplemented,
                notSupportedEx.Message,
                "NotSupported",
                null
            ),

            // Default case for unknown exceptions
            _ => (
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred",
                "InternalError",
                null
            )
        };
    }
}