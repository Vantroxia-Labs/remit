namespace AegisEInvoicing.NotificationService.Models;

public class AwsSesConfiguration
{
    public const string SectionName = "AwsSes";

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";
    public string DefaultFromEmail { get; set; } = string.Empty;
    public string DefaultFromName { get; set; } = string.Empty;
    public bool UseSandbox { get; set; } = false;
    public int MaxRetries { get; set; } = 3;
    public TimeSpan TimeoutSeconds { get; set; } = TimeSpan.FromSeconds(30);
}
