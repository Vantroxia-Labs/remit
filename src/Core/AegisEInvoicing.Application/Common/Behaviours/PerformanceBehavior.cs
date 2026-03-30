using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AegisEInvoicing.Application.Common.Behaviours;

/// <summary>
/// Pipeline behavior for monitoring performance
/// </summary>
public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    ICurrentUserService currentUserService) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger = logger;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId?.ToString() ?? "Anonymous";
            var userName = _currentUserService.UserName ?? "Unknown";

            _logger.LogWarning(
                "Long Running Request: {RequestName} ({ElapsedMilliseconds} ms) by {UserName} ({UserId})",
                requestName, elapsedMilliseconds, userName, userId);
        }

        return response;
    }
}