using System.Diagnostics;
using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services.Telemetry;

/// <summary>
/// Implementation of telemetry service using structured logging and OpenTelemetry ActivitySource.
/// Events and metrics are emitted as structured log entries and OTel activities,
/// which SigNoz captures automatically via the OTLP exporter.
/// </summary>
public class SigNozTelemetryService : ITelemetryService
{
    private static readonly ActivitySource ActivitySource = new("AegisEInvoicing");
    private readonly ILogger<SigNozTelemetryService> _logger;

    public SigNozTelemetryService(ILogger<SigNozTelemetryService> logger)
    {
        _logger = logger;
    }

    public void TrackInvoiceCreated(Guid invoiceId, Guid businessId, string irn, TimeSpan duration)
    {
        using var activity = ActivitySource.StartActivity("InvoiceCreated");
        activity?.SetTag("invoice.id", invoiceId)
                 .SetTag("business.id", businessId)
                 .SetTag("invoice.irn", irn)
                 .SetTag("duration.ms", duration.TotalMilliseconds);

        _logger.LogInformation("Invoice created. InvoiceId={InvoiceId} BusinessId={BusinessId} IRN={IRN} DurationMs={DurationMs}",
            invoiceId, businessId, irn, duration.TotalMilliseconds);
    }

    public void TrackInvoiceValidated(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null)
    {
        using var activity = ActivitySource.StartActivity("InvoiceValidated");
        activity?.SetTag("invoice.id", invoiceId)
                 .SetTag("success", success)
                 .SetTag("duration.ms", duration.TotalMilliseconds);
        if (errorMessage != null) activity?.SetTag("error.message", errorMessage);

        if (success)
            _logger.LogInformation("Invoice validated. InvoiceId={InvoiceId} DurationMs={DurationMs}", invoiceId, duration.TotalMilliseconds);
        else
            _logger.LogWarning("Invoice validation failed. InvoiceId={InvoiceId} Error={Error} DurationMs={DurationMs}", invoiceId, errorMessage, duration.TotalMilliseconds);
    }

    public void TrackInvoiceSigned(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null)
    {
        using var activity = ActivitySource.StartActivity("InvoiceSigned");
        activity?.SetTag("invoice.id", invoiceId)
                 .SetTag("success", success)
                 .SetTag("duration.ms", duration.TotalMilliseconds);
        if (errorMessage != null) activity?.SetTag("error.message", errorMessage);

        if (success)
            _logger.LogInformation("Invoice signed. InvoiceId={InvoiceId} DurationMs={DurationMs}", invoiceId, duration.TotalMilliseconds);
        else
            _logger.LogWarning("Invoice signing failed. InvoiceId={InvoiceId} Error={Error} DurationMs={DurationMs}", invoiceId, errorMessage, duration.TotalMilliseconds);
    }

    public void TrackInvoiceTransmitted(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null)
    {
        using var activity = ActivitySource.StartActivity("InvoiceTransmitted");
        activity?.SetTag("invoice.id", invoiceId)
                 .SetTag("success", success)
                 .SetTag("duration.ms", duration.TotalMilliseconds);
        if (errorMessage != null) activity?.SetTag("error.message", errorMessage);

        if (success)
            _logger.LogInformation("Invoice transmitted. InvoiceId={InvoiceId} DurationMs={DurationMs}", invoiceId, duration.TotalMilliseconds);
        else
            _logger.LogWarning("Invoice transmission failed. InvoiceId={InvoiceId} Error={Error} DurationMs={DurationMs}", invoiceId, errorMessage, duration.TotalMilliseconds);
    }

    public void TrackDependency(
        string dependencyType,
        string serviceName,
        string operationName,
        TimeSpan duration,
        bool success,
        int? statusCode = null,
        string? errorMessage = null)
    {
        using var activity = ActivitySource.StartActivity($"Dependency.{operationName}");
        activity?.SetTag("dependency.type", dependencyType)
                 .SetTag("dependency.service", serviceName)
                 .SetTag("duration.ms", duration.TotalMilliseconds)
                 .SetTag("success", success);
        if (statusCode.HasValue) activity?.SetTag("http.status_code", statusCode.Value);
        if (errorMessage != null) activity?.SetTag("error.message", errorMessage);

        if (success)
            _logger.LogInformation("Dependency call succeeded. Type={Type} Service={Service} Operation={Operation} DurationMs={DurationMs}",
                dependencyType, serviceName, operationName, duration.TotalMilliseconds);
        else
            _logger.LogWarning("Dependency call failed. Type={Type} Service={Service} Operation={Operation} StatusCode={StatusCode} Error={Error}",
                dependencyType, serviceName, operationName, statusCode, errorMessage);
    }

    public void TrackLicenseGenerated(Guid businessId, DateTime expiryDate, bool success)
    {
        using var activity = ActivitySource.StartActivity("LicenseGenerated");
        activity?.SetTag("business.id", businessId)
                 .SetTag("license.expiry", expiryDate.ToString("yyyy-MM-dd"))
                 .SetTag("success", success);

        _logger.LogInformation("License generated. BusinessId={BusinessId} Expiry={Expiry} Success={Success}",
            businessId, expiryDate.ToString("yyyy-MM-dd"), success);
    }

    public void TrackLicenseValidated(Guid businessId, bool success, bool isFailOpen)
    {
        using var activity = ActivitySource.StartActivity("LicenseValidated");
        activity?.SetTag("business.id", businessId)
                 .SetTag("success", success)
                 .SetTag("fail_open", isFailOpen);

        _logger.LogInformation("License validated. BusinessId={BusinessId} Success={Success} FailOpen={FailOpen}",
            businessId, success, isFailOpen);
    }

    public void TrackUserLogin(Guid userId, Guid? businessId, bool success, string? errorMessage = null)
    {
        using var activity = ActivitySource.StartActivity("UserLogin");
        activity?.SetTag("user.id", userId)
                 .SetTag("success", success);
        if (businessId.HasValue) activity?.SetTag("business.id", businessId.Value);
        if (errorMessage != null) activity?.SetTag("error.message", errorMessage);

        if (success)
            _logger.LogInformation("User login succeeded. UserId={UserId} BusinessId={BusinessId}", userId, businessId);
        else
            _logger.LogWarning("User login failed. UserId={UserId} Error={Error}", userId, errorMessage);
    }

    public void TrackSessionCreated(Guid sessionId, Guid userId)
    {
        using var activity = ActivitySource.StartActivity("SessionCreated");
        activity?.SetTag("session.id", sessionId)
                 .SetTag("user.id", userId);

        _logger.LogInformation("Session created. SessionId={SessionId} UserId={UserId}", sessionId, userId);
    }

    public void TrackSessionTerminated(Guid sessionId, string reason)
    {
        using var activity = ActivitySource.StartActivity("SessionTerminated");
        activity?.SetTag("session.id", sessionId)
                 .SetTag("reason", reason);

        _logger.LogInformation("Session terminated. SessionId={SessionId} Reason={Reason}", sessionId, reason);
    }

    public void TrackApiKeyUsage(Guid businessId, string endpoint)
    {
        using var activity = ActivitySource.StartActivity("ApiKeyUsed");
        activity?.SetTag("business.id", businessId)
                 .SetTag("endpoint", endpoint);

        _logger.LogInformation("API key used. BusinessId={BusinessId} Endpoint={Endpoint}", businessId, endpoint);
    }

    public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        using var activity = ActivitySource.StartActivity($"Metric.{metricName}");
        activity?.SetTag("metric.name", metricName)
                 .SetTag("metric.value", value);
        if (properties != null)
            foreach (var prop in properties)
                activity?.SetTag(prop.Key, prop.Value);

        _logger.LogInformation("Metric recorded. Metric={Metric} Value={Value}", metricName, value);
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        using var activity = ActivitySource.StartActivity(eventName);
        if (properties != null)
            foreach (var prop in properties)
                activity?.SetTag(prop.Key, prop.Value);
        if (metrics != null)
            foreach (var metric in metrics)
                activity?.SetTag(metric.Key, metric.Value);

        _logger.LogInformation("Event tracked. Event={Event}", eventName);
    }

    public void TrackPipelineExecution(
        Guid invoiceId,
        bool success,
        string? failedAt,
        TimeSpan totalDuration,
        TimeSpan? createDuration = null,
        TimeSpan? validateDuration = null,
        TimeSpan? signDuration = null,
        TimeSpan? transmitDuration = null)
    {
        using var activity = ActivitySource.StartActivity("InvoicePipelineExecution");
        activity?.SetTag("invoice.id", invoiceId)
                 .SetTag("success", success)
                 .SetTag("total.duration.ms", totalDuration.TotalMilliseconds);
        if (failedAt != null) activity?.SetTag("failed.at", failedAt);
        if (createDuration.HasValue) activity?.SetTag("create.duration.ms", createDuration.Value.TotalMilliseconds);
        if (validateDuration.HasValue) activity?.SetTag("validate.duration.ms", validateDuration.Value.TotalMilliseconds);
        if (signDuration.HasValue) activity?.SetTag("sign.duration.ms", signDuration.Value.TotalMilliseconds);
        if (transmitDuration.HasValue) activity?.SetTag("transmit.duration.ms", transmitDuration.Value.TotalMilliseconds);

        if (success)
            _logger.LogInformation("Invoice pipeline completed. InvoiceId={InvoiceId} TotalDurationMs={DurationMs}",
                invoiceId, totalDuration.TotalMilliseconds);
        else
            _logger.LogWarning("Invoice pipeline failed. InvoiceId={InvoiceId} FailedAt={FailedAt} TotalDurationMs={DurationMs}",
                invoiceId, failedAt, totalDuration.TotalMilliseconds);
    }
}
