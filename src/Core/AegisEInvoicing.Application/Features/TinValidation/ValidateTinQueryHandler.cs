using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Constants;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.Application.Features.TinValidation;

/// <summary>
/// Handler for TIN validation query
/// Validates TIN format and checks MBS enrollment status via APP provider
/// </summary>
public sealed class ValidateTinQueryHandler(
    IAppProviderRouter appProviderRouter,
    ILogger<ValidateTinQueryHandler> logger)
    : IRequestHandler<ValidateTinQuery, TinValidationResult>
{
    private readonly IAppProviderRouter _appProviderRouter = appProviderRouter;
    private readonly ILogger<ValidateTinQueryHandler> _logger = logger;

    public async Task<TinValidationResult> Handle(ValidateTinQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate TIN format (basic validation)
            if (string.IsNullOrWhiteSpace(request.Tin))
            {
                return TinValidationResult.InvalidOrNotEnrolled("TIN cannot be empty");
            }

            // Nigerian TIN format: 12 digits (can have hyphens)
            var tinDigitsOnly = new string(request.Tin.Where(char.IsDigit).ToArray());
            if (tinDigitsOnly.Length != 12)
            {
                return TinValidationResult.InvalidOrNotEnrolled(
                    ResponseMessages.INVALID_TIN_OR_NOT_ENROLLED);
            }

            // Get provider adapter for TIN lookup
            // Using a default business ID since TIN validation doesn't require specific business context
            var provider = await _appProviderRouter.GetProviderAsync(Guid.Empty, cancellationToken);

            if (!provider.SupportsLookupTin)
            {
                _logger.LogWarning("Provider {Provider} does not support TIN lookup", provider.DisplayName);
                return TinValidationResult.Error($"TIN validation not supported by current provider ({provider.DisplayName})");
            }

            // Call provider TIN lookup
            _logger.LogInformation("Validating TIN: {Tin} (masked) using provider {Provider}",
                MaskTin(request.Tin), provider.DisplayName);

            var lookupResult = await provider.LookupTinAsync(request.Tin, cancellationToken);

            // Parse the response
            if (!lookupResult.IsSuccess)
            {
                _logger.LogWarning("TIN lookup failed for TIN: {Tin} - {ErrorMessage}",
                    MaskTin(request.Tin), lookupResult.ErrorMessage);
                return TinValidationResult.Error(lookupResult.ErrorMessage ?? "TIN validation service unavailable");
            }

            // Check if buyer is enrolled (IsUp == true means enrolled)
            if (lookupResult.IsUp)
            {
                _logger.LogInformation(
                    "TIN {Tin} is valid and enrolled. Business: {BusinessRef}",
                    MaskTin(request.Tin),
                    lookupResult.BusinessReference ?? "N/A");

                return TinValidationResult.ValidAndEnrolled(
                    businessName: lookupResult.BusinessReference ?? "Unknown",
                    businessReference: lookupResult.BusinessReference,
                    appReference: null,
                    hasWebhookSetup: false);
            }
            else
            {
                // up == false means either invalid TIN or not enrolled
                // We cannot distinguish between these two cases
                _logger.LogWarning("TIN {Tin} is invalid or not enrolled on MBS portal", MaskTin(request.Tin));

                return TinValidationResult.InvalidOrNotEnrolled(
                    ResponseMessages.INVALID_TIN_OR_NOT_ENROLLED);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TIN: {Tin}", MaskTin(request.Tin));
            return TinValidationResult.Error($"TIN validation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Mask TIN for logging (show only last 4 digits)
    /// </summary>
    private static string MaskTin(string tin)
    {
        if (string.IsNullOrWhiteSpace(tin) || tin.Length < 4)
            return "****";

        return $"***********{tin[^4..]}";
    }
}
