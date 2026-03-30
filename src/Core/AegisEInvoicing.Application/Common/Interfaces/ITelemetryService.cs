namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Service for tracking custom telemetry events and metrics to Application Insights
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Track invoice created event
    /// </summary>
    void TrackInvoiceCreated(Guid invoiceId, Guid businessId, string irn, TimeSpan duration);

    /// <summary>
    /// Track invoice validation completion
    /// </summary>
    void TrackInvoiceValidated(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null);

    /// <summary>
    /// Track invoice signing completion
    /// </summary>
    void TrackInvoiceSigned(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null);

    /// <summary>
    /// Track invoice transmission completion
    /// </summary>
    void TrackInvoiceTransmitted(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null);

    /// <summary>
    /// Track external API dependency call
    /// </summary>
    void TrackDependency(
        string dependencyType, 
        string serviceName, 
        string operationName, 
        TimeSpan duration, 
        bool success, 
        int? statusCode = null,
        string? errorMessage = null);

    /// <summary>
    /// Track license generated event
    /// </summary>
    void TrackLicenseGenerated(Guid businessId, DateTime expiryDate, bool success);

    /// <summary>
    /// Track license validated event
    /// </summary>
    void TrackLicenseValidated(Guid businessId, bool success, bool isFailOpen);

    /// <summary>
    /// Track user login event
    /// </summary>
    void TrackUserLogin(Guid userId, Guid? businessId, bool success, string? errorMessage = null);

    /// <summary>
    /// Track session created event
    /// </summary>
    void TrackSessionCreated(Guid sessionId, Guid userId);

    /// <summary>
    /// Track session terminated event
    /// </summary>
    void TrackSessionTerminated(Guid sessionId, string reason);

    /// <summary>
    /// Track API key usage
    /// </summary>
    void TrackApiKeyUsage(Guid businessId, string endpoint);

    /// <summary>
    /// Track custom metric
    /// </summary>
    void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null);

    /// <summary>
    /// Track custom event
    /// </summary>
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);

    /// <summary>
    /// Track pipeline execution (end-to-end invoice submission)
    /// </summary>
    void TrackPipelineExecution(
        Guid invoiceId, 
        bool success, 
        string? failedAt, 
        TimeSpan totalDuration,
        TimeSpan? createDuration = null,
        TimeSpan? validateDuration = null,
        TimeSpan? signDuration = null,
        TimeSpan? transmitDuration = null);
}
