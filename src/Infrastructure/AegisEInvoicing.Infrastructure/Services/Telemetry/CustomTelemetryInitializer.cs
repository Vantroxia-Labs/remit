using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;

namespace AegisEInvoicing.Infrastructure.Services.Telemetry;

/// <summary>
/// Custom telemetry enricher to add common properties to telemetry.
/// In Application Insights 3.0.0, ITelemetryInitializer was removed.
/// Use ConfigureTelemetryModule or set properties directly via TelemetryClient.Context.
/// </summary>
public class CustomTelemetryInitializer
{
    private readonly string _cloudRoleName;
    private readonly string _applicationVersion;

    public CustomTelemetryInitializer(string cloudRoleName, string? applicationVersion = null)
    {
        _cloudRoleName = cloudRoleName;
        _applicationVersion = applicationVersion ?? "1.0.0";
    }

    /// <summary>
    /// Configures the TelemetryClient with custom properties.
    /// Call this method after obtaining the TelemetryClient from DI.
    /// </summary>
    public void ConfigureTelemetryClient(TelemetryClient telemetryClient)
    {
        telemetryClient.Context.GlobalProperties.TryAdd("CloudRoleName", _cloudRoleName);
        telemetryClient.Context.GlobalProperties.TryAdd("ComponentVersion", _applicationVersion);
        telemetryClient.Context.GlobalProperties.TryAdd("Application", _cloudRoleName);
    }

    /// <summary>
    /// Enriches a single telemetry item with custom properties.
    /// </summary>
    public void EnrichTelemetry(ISupportProperties telemetry)
    {
        telemetry.Properties.TryAdd("CloudRoleName", _cloudRoleName);
        telemetry.Properties.TryAdd("ComponentVersion", _applicationVersion);
        telemetry.Properties.TryAdd("Application", _cloudRoleName);
    }

    public string CloudRoleName => _cloudRoleName;
    public string ApplicationVersion => _applicationVersion;
}
