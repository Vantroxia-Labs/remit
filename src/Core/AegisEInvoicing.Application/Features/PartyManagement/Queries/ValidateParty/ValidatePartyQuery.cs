using MediatR;

namespace AegisEInvoicing.Application.Features.PartyManagement.Queries.ValidateParty;

public sealed record ValidatePartyQuery(Dictionary<string, string> ValidationFields) : IRequest<Dictionary<string, bool>>;
