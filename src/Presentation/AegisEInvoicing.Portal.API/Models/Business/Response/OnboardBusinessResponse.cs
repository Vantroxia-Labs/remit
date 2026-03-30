namespace AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Response;

public class OnboardBusinessResponse
{
    public Guid BusinessId { get; set; }
    public string ConnectionStatus { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
