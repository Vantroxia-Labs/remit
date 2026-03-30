using AegisEInvoicing.NotificationService.Models;

namespace AegisEInvoicing.NotificationService.Extensions;

public static class AwsSesConfigurationExtensions
{
    public static AwsSesConfiguration ParseConnectionString(string connectionString)
    {
        var parameters = connectionString
            .Split(';')
            .Where(part => !string.IsNullOrEmpty(part))
            .Select(part => part.Split('=', 2))
            .Where(split => split.Length == 2)
            .ToDictionary(split => split[0].Trim(), split => split[1].Trim());

        return new AwsSesConfiguration
        {
            AccessKey = parameters.GetValueOrDefault("AccessKey", ""),
            SecretKey = parameters.GetValueOrDefault("SecretKey", ""),
            Region = parameters.GetValueOrDefault("Region", "us-east-1"),
            DefaultFromEmail = parameters.GetValueOrDefault("DefaultFromEmail", ""),
            DefaultFromName = parameters.GetValueOrDefault("DefaultFromName", ""),
            UseSandbox = bool.Parse(parameters.GetValueOrDefault("UseSandbox", "false")),
            MaxRetries = int.Parse(parameters.GetValueOrDefault("MaxRetries", "3")),
            TimeoutSeconds = TimeSpan.FromSeconds(int.Parse(parameters.GetValueOrDefault("TimeoutSeconds", "30")))
        };
    }
}