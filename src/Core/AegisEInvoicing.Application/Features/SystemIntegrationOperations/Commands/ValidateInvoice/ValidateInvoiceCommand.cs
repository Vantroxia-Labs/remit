using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ValidateInvoice;
using AegisEInvoicing.FIRSAccessPoint.Models.Requests.ValidateInvoiceData;
using MediatR;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.ValidateInvoice;

public sealed record ValidateInvoiceCommand(ValidateInvoiceDataRequest ValidateInvoiceData)
    : IRequest<ValidateInvoiceResult>;
