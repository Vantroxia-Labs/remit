using MediatR;

namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ToggleSftpInvoiceTransmission;

public sealed record ToggleSftpInvoiceTransmissionCommand(bool Enabled) : IRequest<ToggleSftpInvoiceTransmissionResult>;
