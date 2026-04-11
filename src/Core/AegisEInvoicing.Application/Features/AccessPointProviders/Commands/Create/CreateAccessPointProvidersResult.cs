namespace AegisEInvoicing.Application.Features.AccessPointProviders.Commands.Create;

public record CreateAccessPointProvidersResult(bool IsSuccess, string Message, Guid? Id = null);
