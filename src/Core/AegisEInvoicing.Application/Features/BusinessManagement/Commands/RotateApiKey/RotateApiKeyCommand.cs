using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.RotateApiKey;

/// <summary>
/// Rotates a business API key after OTP verification.
/// </summary>
public record RotateApiKeyCommand(string Otp) : IRequest<RotateApiKeyResult>;
