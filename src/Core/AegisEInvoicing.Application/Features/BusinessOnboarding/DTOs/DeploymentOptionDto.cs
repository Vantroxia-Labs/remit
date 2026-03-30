using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Application.Features.BusinessOnboarding.DTOs;

public record DeploymentOptionDto
{
    public BusinessDeploymentType Type { get; init; }
    public string Name { get; init; } = default!;
    public string Description { get; init; } = default!;
    public List<string> Requirements { get; init; } = new();
    public string EstimatedSetupTime { get; init; } = default!;
    public string MonthlyCost { get; init; } = default!;
}
