namespace AegisEInvoicing.Infrastructure.Services.EventBus;

/// <summary>
/// Event bus configuration settings
/// </summary>
public sealed record EventBusSettings
{
    public const string SectionName = "EventBus";

    public string ConnectionString { get; set; } = default!;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public int RetryCount { get; set; } = 3;
    public int RetryIntervalSeconds { get; set; } = 5;
    public bool UseOutboxPattern { get; set; } = true;
    public int OutboxProcessingIntervalSeconds { get; set; } = 30;
}