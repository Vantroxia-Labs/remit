using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.UpdateBusiness;

public record UpdateBusinessCommand(
    Guid BusinessId,
    Address RegisteredAddress,
    string InvoicePrefix,
    string Industry,
    string ContactEmail,
    string ContactPhone,
    string Description) : IRequest<UpdateBusinessResult>, ITransactionalCommand;
