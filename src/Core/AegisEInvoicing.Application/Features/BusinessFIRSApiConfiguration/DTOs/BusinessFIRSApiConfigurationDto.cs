using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;

public record BusinessFIRSApiConfigurationDto
{
    public Guid Id { get; init; }
    public Guid BusinessId { get; init; }
    public Guid FIRSApiConfigurationId { get; init; }
    public string BusinessName { get; init; } = default!;
    public string ConfigurationName { get; init; } = default!;
    public string ConfigurationDescription { get; init; } = default!;
    public FIRSDeploymentType DeploymentType { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}