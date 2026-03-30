using AegisEInvoicing.Portal.API.Models;
using AegisEInvoicing.Domain.Exceptions;
using FluentValidation;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Portal.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred");

        context.Response.ContentType = "application/json";

        var response = new ApiResponse<object>
        {
            Success = false,
            TraceId = Activity.Current?.Id ?? context.TraceIdentifier
        };

        switch (exception)
        {
            case ValidationException validationException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = "Validation failed";
                response.Errors = validationException.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage).ToArray());
                break;

            case NotFoundException notFoundException:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                response.Message = notFoundException.Message;
                break;

            case BadRequestException businessException:
                context.Response.StatusCode = StatusCodes.Status422UnprocessableEntity;
                response.Message = businessException.Message;
                break;

            case ConflictException conflictException:
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                response.Message = conflictException.Message;
                break;

            case ForbiddenException:
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                response.Message = "Access forbidden";
                break;

            case AppException domainException:
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                response.Message = domainException.Message;
                break;

            case UnauthorizedAccessException:
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                response.Message = "Unauthorized access";
                break;

            default:
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                response.Message = "An error occurred while processing your request";

                if (_environment.IsDevelopment())
                {
                    response.DeveloperMessage = exception.ToString();
                }
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

// Extension method to use the middleware
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}