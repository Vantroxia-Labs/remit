namespace AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;

public record AccessPointProvidersDto(Guid ConfigurationId, string Name, string Description, string ApiKey, string ApiSecret, string env, string baseUrl);
