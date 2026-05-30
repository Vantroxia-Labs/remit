using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.GetApiCredentials;

/// <summary>
/// Returns the current business's API access credentials including the masked API key,
/// ERP API base URL, and required request headers.
/// </summary>
public record GetApiCredentialsQuery : IRequest<GetApiCredentialsResult>;
