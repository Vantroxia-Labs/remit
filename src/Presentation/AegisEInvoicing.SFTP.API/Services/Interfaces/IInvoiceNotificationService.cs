using AegisEInvoicing.SFTP.API.Models;

namespace AegisEInvoicing.SFTP.API.Services.Interfaces;

/// <summary>
/// Service for sending invoice-related notifications
/// </summary>
public interface IInvoiceNotificationService
{
    /// <summary>
    /// Sends a success notification for a successfully processed invoice
    /// </summary>
    /// <param name="invoiceId">The ID of the processed invoice</param>
    /// <param name="partyId">The party ID associated with the invoice</param>
    /// <param name="irn">The Invoice Reference Number</param>
    /// <param name="fileName">The original file name</param>
    /// <param name="connectionId">The SFTP connection ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if notification was sent successfully</returns>
    Task<bool> SendInvoiceSuccessNotificationAsync(
        Guid invoiceId,
        Guid partyId,
        string irn,
        string fileName,
        string connectionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an error notification for a failed invoice processing
    /// </summary>
    /// <param name="fileName">The original file name</param>
    /// <param name="connectionId">The SFTP connection ID</param>
    /// <param name="errorMessage">The error message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if notification was sent successfully</returns>
    Task<bool> SendInvoiceErrorNotificationAsync(
        string fileName,
        string connectionId,
        string errorMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a summary notification with processing statistics
    /// </summary>
    /// <param name="statistics">Processing statistics</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if notification was sent successfully</returns>
    Task<bool> SendProcessingSummaryNotificationAsync(
        ProcessingStatistics statistics,
        CancellationToken cancellationToken = default);
}