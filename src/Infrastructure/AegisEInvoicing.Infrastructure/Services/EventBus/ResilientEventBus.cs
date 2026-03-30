using Ardalis.GuardClauses;
using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services.EventBus;

/// <summary>
/// Resilient event bus with RabbitMQ and fallback to database outbox
/// </summary>
public sealed class ResilientEventBus : IEventBus
{
    private readonly IBus? _massTransitBus;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResilientEventBus> _logger;
    private readonly EventBusSettings _settings;

    public ResilientEventBus(
        IBus? massTransitBus,
        IApplicationDbContext context,
        IOptions<EventBusSettings> settings,
        ILogger<ResilientEventBus> logger)
    {
        _massTransitBus = massTransitBus;
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent
    {
        Guard.Against.Null(@event, nameof(@event));

        var eventName = @event.GetType().Name;
        var eventData = JsonSerializer.Serialize(@event);

        try
        {
            // Try to publish via MassTransit/RabbitMQ
            if (_massTransitBus != null && await IsRabbitMqHealthyAsync())
            {
                await _massTransitBus.Publish(@event, cancellationToken);

                _logger.LogInformation(
                    "Event {EventName} published to RabbitMQ successfully",
                    eventName);

                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish event {EventName} to RabbitMQ, falling back to outbox",
                eventName);
        }

        // Fallback to outbox pattern
        await SaveToOutboxAsync(@event, cancellationToken);
    }

    public async Task PublishBatchAsync<TEvent>(
        IEnumerable<TEvent> events,
        CancellationToken cancellationToken = default) where TEvent : class, IDomainEvent
    {
        var eventsList = events.ToList();

        if (!eventsList.Any())
            return;

        try
        {
            if (_massTransitBus != null && await IsRabbitMqHealthyAsync())
            {
                await _massTransitBus.PublishBatch(eventsList, cancellationToken);

                _logger.LogInformation(
                    "Batch of {Count} events published to RabbitMQ successfully",
                    eventsList.Count);

                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish batch of {Count} events to RabbitMQ, falling back to outbox",
                eventsList.Count);
        }

        // Fallback to outbox pattern
        foreach (var @event in eventsList)
        {
            await SaveToOutboxAsync(@event, cancellationToken);
        }
    }

    private async Task SaveToOutboxAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var outboxEvent = OutboxEvent.Create(
            @event.GetType().AssemblyQualifiedName!,
            JsonSerializer.Serialize(@event));

        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Event {EventType} saved to outbox with ID {EventId}",
            @event.GetType().Name,
            outboxEvent.Id);
    }

    private async Task<bool> IsRabbitMqHealthyAsync()
    {
        try
        {
            // Simple health check - this would be more sophisticated in production
            return _massTransitBus != null && await Task.FromResult(true);
        }
        catch
        {
            return false;
        }
    }
}