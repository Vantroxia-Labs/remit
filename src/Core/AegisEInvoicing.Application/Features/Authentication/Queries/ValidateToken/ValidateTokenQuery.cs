using MediatR;

namespace AegisEInvoicing.Application.Features.Authentication.Queries.ValidateToken;

public record ValidateTokenQuery(string Token) : IRequest<TokenValidationResult>;

public record TokenValidationResult(
    bool IsValid,
    Guid? UserId,
    string? ErrorMessage,
    DateTimeOffset? ExpiresAt);