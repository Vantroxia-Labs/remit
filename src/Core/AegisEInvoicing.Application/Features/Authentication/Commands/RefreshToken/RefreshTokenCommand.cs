using MediatR;

namespace AegisEInvoicing.Application.Features.Authentication.Commands.RefreshToken;

public record RefreshTokenCommand(
    string RefreshToken,
    string IpAddress) : IRequest<RefreshTokenResult>;