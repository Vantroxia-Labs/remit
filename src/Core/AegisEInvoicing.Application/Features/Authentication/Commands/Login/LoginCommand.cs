using MediatR;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Login;

public record LoginCommand(
  string Email,
  string Password,
  string IpAddress,
  string UserAgent) : IRequest<LoginResult>;
