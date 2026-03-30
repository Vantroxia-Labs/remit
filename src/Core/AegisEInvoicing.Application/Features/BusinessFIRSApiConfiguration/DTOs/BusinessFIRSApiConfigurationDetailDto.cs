namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.DTOs;

public record BusinessFIRSApiConfigurationDetailDto
{
    public string ConfigurationName { get; init; } = default!;
    public string ConfigurationDescription { get; init; } = default!;
}