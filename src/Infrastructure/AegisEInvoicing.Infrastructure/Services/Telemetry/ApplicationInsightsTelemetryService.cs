using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Infrastructure.Services.Telemetry;

/// <summary>
/// Implementation of telemetry service using Azure Application Insights
/// </summary>
public class ApplicationInsightsTelemetryService : ITelemetryService
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<ApplicationInsightsTelemetryService> _logger;
    private readonly bool _isEnabled;

    public ApplicationInsightsTelemetryService(
        TelemetryClient? telemetryClient,
        ILogger<ApplicationInsightsTelemetryService> logger)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
        _isEnabled = telemetryClient != null;

        if (!_isEnabled)
        {
            _logger.LogWarning("Application Insights TelemetryClient is not configured. Telemetry tracking will be disabled.");
        }
    }

    /// <summary>
    /// Helper method to track events with properties and metrics (App Insights 3.0.0 compatible)
    /// In 3.0.0, EventTelemetry.Metrics was removed - metrics are now added as string properties
    /// </summary>
    private void TrackEventInternal(string eventName, IDictionary<string, string>? properties, IDictionary<string, double>? metrics)
    {
        var eventTelemetry = new EventTelemetry(eventName);

        if (properties != null)
        {
            foreach (var prop in properties)
            {
                eventTelemetry.Properties[prop.Key] = prop.Value;
            }
        }

        // In App Insights 3.0.0, metrics are added as properties (converted to string)
        if (metrics != null)
        {
            foreach (var metric in metrics)
            {
                eventTelemetry.Properties[metric.Key] = metric.Value.ToString("F2");
            }
        }

        _telemetryClient!.TrackEvent(eventTelemetry);
    }

    public void TrackInvoiceCreated(Guid invoiceId, Guid businessId, string irn, TimeSpan duration)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["InvoiceId"] = invoiceId.ToString(),
                ["BusinessId"] = businessId.ToString(),
                ["IRN"] = irn,
                ["Stage"] = "Create"
            };

            var metrics = new Dictionary<string, double>
            {
                ["DurationMs"] = duration.TotalMilliseconds
            };

            TrackEventInternal("InvoiceCreated", properties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track InvoiceCreated telemetry");
        }
    }

    public void TrackInvoiceValidated(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["InvoiceId"] = invoiceId.ToString(),
                ["Success"] = success.ToString(),
                ["Stage"] = "Validate"
            };

            if (!string.IsNullOrEmpty(errorMessage))
                properties["ErrorMessage"] = errorMessage;

            var metrics = new Dictionary<string, double>
            {
                ["DurationMs"] = duration.TotalMilliseconds,
                ["SuccessFlag"] = success ? 1 : 0
            };

            TrackEventInternal("InvoiceValidated", properties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track InvoiceValidated telemetry");
        }
    }

    public void TrackInvoiceSigned(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["InvoiceId"] = invoiceId.ToString(),
                ["Success"] = success.ToString(),
                ["Stage"] = "Sign"
            };

            if (!string.IsNullOrEmpty(errorMessage))
                properties["ErrorMessage"] = errorMessage;

            var metrics = new Dictionary<string, double>
            {
                ["DurationMs"] = duration.TotalMilliseconds,
                ["SuccessFlag"] = success ? 1 : 0
            };

            TrackEventInternal("InvoiceSigned", properties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track InvoiceSigned telemetry");
        }
    }

    public void TrackInvoiceTransmitted(Guid invoiceId, bool success, TimeSpan duration, string? errorMessage = null)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["InvoiceId"] = invoiceId.ToString(),
                ["Success"] = success.ToString(),
                ["Stage"] = "Transmit"
            };

            if (!string.IsNullOrEmpty(errorMessage))
                properties["ErrorMessage"] = errorMessage;

            var metrics = new Dictionary<string, double>
            {
                ["DurationMs"] = duration.TotalMilliseconds,
                ["SuccessFlag"] = success ? 1 : 0
            };

            TrackEventInternal("InvoiceTransmitted", properties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track InvoiceTransmitted telemetry");
        }
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
        if (!_isEnabled) return;

        try
        {
            var dependency = new DependencyTelemetry
            {
                Type = dependencyType,
                Target = serviceName,
                Name = operationName,
                Duration = duration,
                Success = success,
                ResultCode = statusCode?.ToString()
            };

            if (!string.IsNullOrEmpty(errorMessage))
                dependency.Properties["ErrorMessage"] = errorMessage;

            _telemetryClient!.TrackDependency(dependency);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track dependency telemetry");
        }
    }

    public void TrackLicenseGenerated(Guid businessId, DateTime expiryDate, bool success)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["BusinessId"] = businessId.ToString(),
                ["ExpiryDate"] = expiryDate.ToString("yyyy-MM-dd"),
                ["Success"] = success.ToString()
            };

            var metrics = new Dictionary<string, double>
            {
                ["DaysUntilExpiry"] = (expiryDate - DateTime.UtcNow).TotalDays
            };

            TrackEventInternal("LicenseGenerated", properties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track LicenseGenerated telemetry");
        }
    }

    public void TrackLicenseValidated(Guid businessId, bool success, bool isFailOpen)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["BusinessId"] = businessId.ToString(),
                ["Success"] = success.ToString(),
                ["IsFailOpen"] = isFailOpen.ToString()
            };

            TrackEventInternal("LicenseValidated", properties, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track LicenseValidated telemetry");
        }
    }

    public void TrackUserLogin(Guid userId, Guid? businessId, bool success, string? errorMessage = null)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["UserId"] = userId.ToString(),
                ["Success"] = success.ToString()
            };

            if (businessId.HasValue)
                properties["BusinessId"] = businessId.Value.ToString();

            if (!string.IsNullOrEmpty(errorMessage))
                properties["ErrorMessage"] = errorMessage;

            TrackEventInternal("UserLogin", properties, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track UserLogin telemetry");
        }
    }

    public void TrackSessionCreated(Guid sessionId, Guid userId)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["SessionId"] = sessionId.ToString(),
                ["UserId"] = userId.ToString()
            };

            TrackEventInternal("SessionCreated", properties, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track SessionCreated telemetry");
        }
    }

    public void TrackSessionTerminated(Guid sessionId, string reason)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["SessionId"] = sessionId.ToString(),
                ["Reason"] = reason
            };

            TrackEventInternal("SessionTerminated", properties, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track SessionTerminated telemetry");
        }
    }

    public void TrackApiKeyUsage(Guid businessId, string endpoint)
    {
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["BusinessId"] = businessId.ToString(),
                ["Endpoint"] = endpoint
            };

            TrackEventInternal("ApiKeyUsed", properties, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track ApiKeyUsage telemetry");
        }
    }

    public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        if (!_isEnabled) return;

        try
        {
            var metricTelemetry = new MetricTelemetry(metricName, value);

            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    metricTelemetry.Properties[prop.Key] = prop.Value;
                }
            }

            _telemetryClient!.TrackMetric(metricTelemetry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track custom metric");
        }
    }

    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        if (!_isEnabled) return;

        try
        {
            TrackEventInternal(eventName, properties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track custom event");
        }
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
        if (!_isEnabled) return;

        try
        {
            var properties = new Dictionary<string, string>
            {
                ["InvoiceId"] = invoiceId.ToString(),
                ["Success"] = success.ToString()
            };

            if (!string.IsNullOrEmpty(failedAt))
                properties["FailedAt"] = failedAt;

            var metrics = new Dictionary<string, double>
            {
                ["TotalDurationMs"] = totalDuration.TotalMilliseconds
            };

            if (createDuration.HasValue)
                metrics["CreateDurationMs"] = createDuration.Value.TotalMilliseconds;

            if (validateDuration.HasValue)
                metrics["ValidateDurationMs"] = validateDuration.Value.TotalMilliseconds;

            if (signDuration.HasValue)
                metrics["SignDurationMs"] = signDuration.Value.TotalMilliseconds;

            if (transmitDuration.HasValue)
                metrics["TransmitDurationMs"] = transmitDuration.Value.TotalMilliseconds;

            TrackEventInternal("InvoicePipelineExecution", properties, metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track pipeline execution telemetry");
        }
    }
}
