using AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.Queries;

/// <summary>
/// Returns a single APP provider with decrypted credentials. AegisAdmin only.
/// </summary>
public record GetAccessPointProviderByIdQuery(Guid Id) : IRequest<AccessPointProviderEditDto?>;
