using MediatR;

namespace AegisEInvoicing.Application.Features.UserManagement.Commands.UserCommands.SendForgotPasswordOTP;

public record ForgotPasswordCommand(string otp, string password, string phone_email) : IRequest<ForgotPasswordResult>;
