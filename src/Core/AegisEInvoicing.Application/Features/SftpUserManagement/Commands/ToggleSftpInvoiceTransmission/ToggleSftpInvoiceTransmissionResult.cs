namespace AegisEInvoicing.Application.Features.SftpUserManagement.Commands.ToggleSftpInvoiceTransmission;

public sealed record ToggleSftpInvoiceTransmissionResult(bool IsSuccess, string Message, Guid? BusinessId, bool? Enabled);
