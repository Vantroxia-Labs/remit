namespace AegisEInvoicing.NotificationService.Models;

public class MailKitConfiguration
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; } = true;
    public string DefaultFromEmail { get; set; } = string.Empty;
    public string DefaultFromName { get; set; } = string.Empty;

    // Enterprise features
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
    public int ConnectionPoolSize { get; set; } = 5;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(60);
    public bool EnableConnectionReuse { get; set; } = true;
    public TimeSpan ConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentOperations { get; set; } = 10;
}