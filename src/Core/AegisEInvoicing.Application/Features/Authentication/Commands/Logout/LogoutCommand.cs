using MediatR;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.Logout;

public record LogoutCommand(
    string? RefreshToken,
    string IpAddress,
    string? AccessToken = null) : IRequest<LogoutResult>;