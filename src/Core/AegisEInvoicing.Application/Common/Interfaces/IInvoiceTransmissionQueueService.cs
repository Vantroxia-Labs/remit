using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for queuing invoice transmission requests
/// </summary>
public interface IInvoiceTransmissionQueueService
{
    /// <summary>
    /// Queue an invoice transmission request for processing
    /// </summary>
    /// <param name="request">The transmission request</param>
    /// <param name="businessId">Business context</param>
    /// <param name="userId">User context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully queued</returns>
    Task<bool> QueueTransmissionRequestAsync(
        InvoiceTransmissionRequest request,
        Guid? businessId = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the number of pending requests in the queue
    /// </summary>
    /// <returns>Number of pending requests</returns>
    Task<int> GetPendingRequestCountAsync();

    /// <summary>
    /// Process pending requests (called by background service)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of requests processed</returns>
    Task<int> ProcessPendingRequestsAsync(CancellationToken cancellationToken = default);
}