using MediatR;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.GenerateIrn;

public sealed record GenerateIrnCommand(string InvoiceNumber, DateOnly IssueDate): IRequest<GenerateIrnResult>;