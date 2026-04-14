using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.SendActionOtp;

/// <summary>
/// Sends a one-time password to the currently authenticated user's email
/// for confirming a sensitive action (e.g. rotating API key, changing SFTP password).
/// </summary>
public record SendActionOtpCommand : IRequest<SendActionOtpResult>;
