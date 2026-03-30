using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.AddFirsApiConfiguration;

public record AddFirsApiConfigurationCommand(
    string FirsApiKey,
    string FirsClientSecret) : IRequest<GenericResult>, ITransactionalCommand;
