namespace AegisEInvoicing.NotificationService.Models;

public class AzureCommunicationConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public string DefaultFromEmail { get; set; } = string.Empty;
    public string DefaultFromName { get; set; } = string.Empty;
    public string DefaultBccEmail { get; set; } = string.Empty;

    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(100);
    public int MaxConcurrentOperations { get; set; } = 10;
    public bool EnableTelemetry { get; set; } = true;
}
