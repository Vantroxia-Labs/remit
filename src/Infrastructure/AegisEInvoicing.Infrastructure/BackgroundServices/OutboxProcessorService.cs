using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Infrastructure.Services.EventBus;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.BackgroundServices;

/// <summary>
/// Background service to process outbox events
/// </summary>
public sealed class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly EventBusSettings _settings;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        IOptions<EventBusSettings> settings,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(_settings.OutboxProcessingIntervalSeconds),
                stoppingToken);
        }

        _logger.LogInformation("Outbox Processor Service stopped");
    }

    private async Task ProcessOutboxEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var eventBus = scope.ServiceProvider.GetService<IBus>();

        if (eventBus == null)
        {
            _logger.LogWarning("Event bus not available, skipping outbox processing");
            return;
        }

        List<OutboxEvent> pendingEvents;

        try
        {
            // Use AsNoTracking for better performance and to avoid entity tracking issues
            // Also use ExecutionStrategy to handle transient database errors
            var strategy = context.Database.CreateExecutionStrategy();
            pendingEvents = await strategy.ExecuteAsync(async () =>
                await context.OutboxEvents
                    .AsNoTracking()
                    .Where(e => e.Status == OutboxEventStatus.Pending && e.RetryCount < 5)
                    .OrderBy(e => e.CreatedAt)
                    .Take(100)
                    .ToListAsync(cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch pending outbox events from database. Will retry on next cycle.");
            return;
        }

        if (!pendingEvents.Any())
            return;

        _logger.LogInformation("Processing {Count} pending outbox events", pendingEvents.Count);

        // Re-attach entities to context for tracking since we used AsNoTracking
        foreach (var evt in pendingEvents)
        {
            context.OutboxEvents.Attach(evt);
        }

        foreach (var outboxEvent in pendingEvents)
        {
            try
            {
                var eventType = Type.GetType(outboxEvent.EventType);
                if (eventType == null)
                {
                    _logger.LogWarning(
                        "Unknown event type {EventType} for outbox event {EventId}",
                        outboxEvent.EventType,
                        outboxEvent.Id);

                    outboxEvent.Status = OutboxEventStatus.Failed;
                    outboxEvent.Error = "Unknown event type";
                    continue;
                }

                var @event = JsonSerializer.Deserialize(outboxEvent.EventData, eventType);
                if (@event == null)
                {
                    _logger.LogWarning(
                        "Failed to deserialize outbox event {EventId}",
                        outboxEvent.Id);

                    outboxEvent.Status = OutboxEventStatus.Failed;
                    outboxEvent.Error = "Deserialization failed";
                    continue;
                }

                await eventBus.Publish(@event, eventType, cancellationToken);

                outboxEvent.Status = OutboxEventStatus.Processed;
                outboxEvent.ProcessedOnUtc = DateTime.UtcNow;

                _logger.LogInformation(
                    "Outbox event {EventId} processed successfully",
                    outboxEvent.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process outbox event {EventId}",
                    outboxEvent.Id);

                outboxEvent.RetryCount++;
                outboxEvent.Error = ex.Message;

                if (outboxEvent.RetryCount >= 5)
                {
                    outboxEvent.Status = OutboxEventStatus.Failed;
                }
            }
        }

        try
        {
            // Use ExecutionStrategy for SaveChanges to handle transient errors
            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
                await context.SaveChangesAsync(cancellationToken));

            _logger.LogInformation("Successfully saved {Count} outbox event updates", pendingEvents.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save outbox event updates to database. Changes will be retried on next cycle.");
        }
    }
}