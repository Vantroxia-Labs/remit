namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for comprehensive audit logging of invoice operations.
/// Provides tamper-proof audit trail for compliance and security.
/// </summary>
public interface IInvoiceAuditService
{
    /// <summary>
    /// Logs invoice state transition with comprehensive audit information
    /// </summary>
    /// <param name="invoiceId">Invoice identifier</param>
    /// <param name="operation">Operation performed (Validate, Sign, Transmit, Approve, etc.)</param>
    /// <param name="statusBefore">Status before operation</param>
    /// <param name="statusAfter">Status after operation</param>
    /// <param name="userId">User who performed the operation</param>
    /// <param name="ipAddress">IP address of the request</param>
    /// <param name="additionalData">Additional context data (JSON)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogStateTransitionAsync(
        Guid invoiceId,
        string operation,
        string statusBefore,
        string statusAfter,
        Guid? userId,
        string? ipAddress,
        string? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs external service interaction for audit trail
    /// </summary>
    /// <param name="invoiceId">Invoice identifier</param>
    /// <param name="serviceName">External service name (FIRS, Interswitch)</param>
    /// <param name="operation">Operation performed</param>
    /// <param name="requestData">Request sent to external service (sanitized)</param>
    /// <param name="responseData">Response received from external service (sanitized)</param>
    /// <param name="isSuccess">Whether the operation succeeded</param>
    /// <param name="responseHash">Hash of complete response for integrity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogExternalServiceInteractionAsync(
        Guid invoiceId,
        string serviceName,
        string operation,
        string? requestData,
        string? responseData,
        bool isSuccess,
        string? responseHash = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs suspicious activity or security events
    /// </summary>
    /// <param name="invoiceId">Invoice identifier (if applicable)</param>
    /// <param name="eventType">Type of security event</param>
    /// <param name="description">Description of the event</param>
    /// <param name="severity">Severity level (Low, Medium, High, Critical)</param>
    /// <param name="ipAddress">IP address involved</param>
    /// <param name="userId">User ID involved (if applicable)</param>
    /// <param name="additionalData">Additional context</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LogSecurityEventAsync(
        Guid? invoiceId,
        string eventType,
        string description,
        string severity,
        string? ipAddress,
        Guid? userId,
        string? additionalData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit trail for a specific invoice
    /// </summary>
    /// <param name="invoiceId">Invoice identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Chronological audit trail</returns>
    Task<List<InvoiceAuditEntry>> GetAuditTrailAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies integrity of audit trail (no tampering)
    /// </summary>
    /// <param name="invoiceId">Invoice identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if audit trail is intact</returns>
    Task<bool> VerifyAuditTrailIntegrityAsync(
        Guid invoiceId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents a single audit log entry
/// </summary>
public class InvoiceAuditEntry
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public DateTime Timestamp { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string? StatusBefore { get; set; }
    public string? StatusAfter { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }
    public string? IpAddress { get; set; }
    public string? AdditionalData { get; set; }
    public string? Signature { get; set; } // HMAC signature for tamper detection
    public string EntryType { get; set; } = "StateTransition"; // StateTransition, ExternalService, SecurityEvent
}
