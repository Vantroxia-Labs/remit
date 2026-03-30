using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries.ValidateBusiness;

public record ValidateBusinessQuery(Dictionary<string, string> ValidationFields) : IRequest<Dictionary<string, bool>>;
