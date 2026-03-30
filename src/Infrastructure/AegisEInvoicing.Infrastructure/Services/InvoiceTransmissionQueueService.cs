using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ProcessInvoiceTransmission;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.Infrastructure.Services;

/// <summary>
/// Service for managing invoice transmission queue
/// </summary>
public class InvoiceTransmissionQueueService : IInvoiceTransmissionQueueService
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;
    private readonly ILogger<InvoiceTransmissionQueueService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public InvoiceTransmissionQueueService(
        IApplicationDbContext context,
        IMediator mediator,
        ILogger<InvoiceTransmissionQueueService> logger)
    {
        _context = context;
        _mediator = mediator;
        _logger = logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<bool> QueueTransmissionRequestAsync(
        InvoiceTransmissionRequest request,
        Guid? businessId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Irn) || !request.Message.HasValue)
            {
                _logger.LogWarning("Invalid transmission request: IRN={IRN}, Message={Message}",
                    request.Irn, request.Message);
                return false;
            }

            var requestPayload = JsonSerializer.Serialize(request, _jsonOptions);

            var queueItem = InvoiceTransmissionQueue.Create(
                request.Irn,
                request.Message.Value,
                requestPayload,
                businessId,
                userId);

            _context.InvoiceTransmissionQueues.Add(queueItem);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Queued transmission request for IRN {IRN} with status {Status}",
                request.Irn, request.Message.Value);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error queuing transmission request for IRN {IRN}", request.Irn);
            return false;
        }
    }

    public async Task<int> GetPendingRequestCountAsync()
    {
        try
        {
            return await _context.InvoiceTransmissionQueues
                .Where(q => q.ProcessingStatus == QueueStatus.Pending &&
                           (q.ProcessAfter == null || q.ProcessAfter <= DateTimeOffset.UtcNow))
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending request count");
            return 0;
        }
    }

    public async Task<int> ProcessPendingRequestsAsync(CancellationToken cancellationToken = default)
    {
        var processedCount = 0;

        try
        {
            var pendingRequests = await _context.InvoiceTransmissionQueues
                .Where(q => q.ProcessingStatus == QueueStatus.Pending &&
                           (q.ProcessAfter == null || q.ProcessAfter <= DateTimeOffset.UtcNow))
                .OrderBy(q => q.CreatedAt)
                .Take(10) // Process in batches
                .ToListAsync(cancellationToken);

            foreach (var request in pendingRequests)
            {
                try
                {
                    await ProcessSingleRequest(request, cancellationToken);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing request {RequestId} for IRN {IRN}",
                        request.Id, request.Irn);

                    request.MarkAsFailed($"Processing error: {ex.Message}");
                    _context.InvoiceTransmissionQueues.Update(request);
                }
            }

            if (processedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Processed {Count} transmission requests", processedCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending transmission requests");
        }

        return processedCount;
    }

    private async Task ProcessSingleRequest(InvoiceTransmissionQueue queueItem, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing transmission request {RequestId} for IRN {IRN}",
            queueItem.Id, queueItem.Irn);

        queueItem.MarkAsProcessing();
        _context.InvoiceTransmissionQueues.Update(queueItem);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            var request = JsonSerializer.Deserialize<InvoiceTransmissionRequest>(
                queueItem.RequestPayload, _jsonOptions);

            if (request == null)
            {
                throw new InvalidOperationException("Failed to deserialize request payload");
            }

            var command = new ProcessInvoiceTransmissionCommand
            {
                Irn = queueItem.Irn,
                Status = queueItem.Status,
                Metadata = null, // TODO: Add metadata support to InvoiceTransmissionRequest
                BusinessId = queueItem.BusinessId,
                UserId = queueItem.UserId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                queueItem.MarkAsCompleted();
                _logger.LogInformation("Successfully processed transmission request {RequestId} for IRN {IRN}",
                    queueItem.Id, queueItem.Irn);
            }
            else
            {
                queueItem.MarkAsFailed(result.ErrorDetails ?? result.Message);
                _logger.LogWarning("Failed to process transmission request {RequestId} for IRN {IRN}: {Error}",
                    queueItem.Id, queueItem.Irn, result.Message);
            }

            _context.InvoiceTransmissionQueues.Update(queueItem);
        }
        catch (Exception ex)
        {
            queueItem.MarkAsFailed($"Processing exception: {ex.Message}");
            _context.InvoiceTransmissionQueues.Update(queueItem);
            throw;
        }
    }
}