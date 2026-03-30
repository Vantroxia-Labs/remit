using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Common.Behaviours;

/// <summary>
/// Pipeline behavior for caching responses
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>, ICacheableQuery
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(
        ICacheService cache,
        ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!request.BypassCache)
        {
            var cachedResponse = await _cache.GetAsync<TResponse>(
                request.CacheKey,
                cancellationToken);

            if (cachedResponse is not null)
            {
                _logger.LogDebug(
                    "Response retrieved from cache for {RequestName} with key {CacheKey}",
                    typeof(TRequest).Name,
                    request.CacheKey);

                return cachedResponse;
            }
        }

        var response = await next();

        if (request.SlidingExpiration.HasValue || request.AbsoluteExpiration.HasValue)
        {
            var expiration = request.SlidingExpiration ??
                TimeSpan.FromMinutes(request.AbsoluteExpirationMinutes ?? 5);

            await _cache.SetAsync(
                request.CacheKey,
                response,
                expiration,
                cancellationToken);

            _logger.LogDebug(
                "Response cached for {RequestName} with key {CacheKey}",
                typeof(TRequest).Name,
                request.CacheKey);
        }

        return response;
    }
}