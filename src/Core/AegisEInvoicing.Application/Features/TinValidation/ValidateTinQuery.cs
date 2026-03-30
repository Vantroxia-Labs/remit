using MediatR;

namespace AegisEInvoicing.Application.Features.TinValidation;

/// <summary>
/// Query to validate TIN and check MBS enrollment status
/// </summary>
public sealed record ValidateTinQuery(string Tin) : IRequest<TinValidationResult>;

/// <summary>
/// Result of TIN validation
/// </summary>
public sealed record TinValidationResult
{
    public bool Success { get; init; }
    public TinValidationStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? BusinessName { get; init; }
    public string? BusinessReference { get; init; }
    public string? AppReference { get; init; }
    public bool? HasWebhookSetup { get; init; }

    public static TinValidationResult ValidAndEnrolled(
        string businessName, 
        string? businessReference = null,
        string? appReference = null,
        bool? hasWebhookSetup = null) => new()
    {
        Success = true,
        Status = TinValidationStatus.ValidAndEnrolled,
        Message = Domain.Constants.ResponseMessages.TIN_VALID_AND_ENROLLED,
        BusinessName = businessName,
        BusinessReference = businessReference,
        AppReference = appReference,
        HasWebhookSetup = hasWebhookSetup
    };

    public static TinValidationResult InvalidOrNotEnrolled(string message) => new()
    {
        Success = false,
        Status = TinValidationStatus.InvalidOrNotEnrolled,
        Message = message
    };

    public static TinValidationResult Error(string message) => new()
    {
        Success = false,
        Status = TinValidationStatus.Error,
        Message = message
    };
}

/// <summary>
/// TIN validation status enum
/// </summary>
public enum TinValidationStatus
{
    /// <summary>
    /// TIN is valid and buyer is enrolled on MBS portal
    /// </summary>
    ValidAndEnrolled,

    /// <summary>
    /// TIN is invalid OR valid but not enrolled on MBS portal
    /// (Combined status since Interswitch doesn't distinguish between these)
    /// </summary>
    InvalidOrNotEnrolled,

    /// <summary>
    /// Error occurred during validation
    /// </summary>
    Error
}
