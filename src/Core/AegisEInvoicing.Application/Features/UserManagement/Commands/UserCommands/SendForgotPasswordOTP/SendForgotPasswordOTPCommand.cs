using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.RequestChangePassword;

public record SendForgotPasswordOTPCommand(string phone_email) : IRequest<SendForgotPasswordOTPResult>;
