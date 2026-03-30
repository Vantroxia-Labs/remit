using AegisEInvoicing.Domain.ValueObjects;
using MediatR;

namespace AegisEInvoicing.Application.Features.SystemIntegrationOperations.Commands.GenerateQrCode;

public sealed record GenerateQrCodeCommand(string Irn) : IRequest<GenerateQrCodeResult>;