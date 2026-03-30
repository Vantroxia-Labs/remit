using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Models;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.AddQrCodeConfiguration;

public record AddQrCodeConfigurationCommand(
    string PublicKey,
    string Certificate) : IRequest<GenericResult>, ITransactionalCommand;