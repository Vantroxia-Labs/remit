using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessFIRSApiConfiguration.Commands.AssignFIRSConfiguration;

public record AssignFIRSConfigurationCommand(Guid FIRSApiConfigurationId)
    : IRequest<AssignFIRSConfigurationResult>, ITransactionalCommand;