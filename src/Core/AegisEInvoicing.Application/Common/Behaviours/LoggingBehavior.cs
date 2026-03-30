using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Common.Behaviours;

/// <summary>
/// Pipeline behavior for logging requests
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUserService,
    IDateTime dateTime) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IDateTime _dateTime = dateTime;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";
        var userName = _currentUserService.UserName ?? "Unknown";

        _logger.LogInformation(
            "Handling {RequestName} by {UserName} ({UserId}) at {DateTime}",
            requestName, userName, userId, _dateTime.UtcNow);

        try
        {
            var response = await next();

            _logger.LogInformation(
                "Handled {RequestName} successfully",
                requestName);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling {RequestName} for {UserName} ({UserId})",
                requestName, userName, userId);
            throw;
        }
    }
}