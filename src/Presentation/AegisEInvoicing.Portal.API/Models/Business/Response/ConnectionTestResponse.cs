namespace AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Response;

public class ConnectionTestResponse
{
    public bool IsConnected { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset LastTestAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
