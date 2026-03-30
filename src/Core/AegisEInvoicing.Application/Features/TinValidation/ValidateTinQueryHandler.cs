using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Interswitch.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AegisEInvoicing.Application.Features.TinValidation;

/// <summary>
/// Handler for TIN validation query
/// Validates TIN format and checks MBS enrollment status via Interswitch
/// </summary>
public sealed class ValidateTinQueryHandler(
    IInterswitchHttpClient interswitchClient,
    ILogger<ValidateTinQueryHandler> logger)
    : IRequestHandler<ValidateTinQuery, TinValidationResult>
{
    private readonly IInterswitchHttpClient _interswitchClient = interswitchClient;
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

            // Call Interswitch TIN lookup
            _logger.LogInformation("Validating TIN: {Tin} (masked)", MaskTin(request.Tin));

            var lookupResponse = await _interswitchClient.LookupWithTINAsync(
                new Interswitch.Models.Requests.LookupWithTIN.LookupWithTINRequest
                {
                    TIN = request.Tin
                }, 
                cancellationToken);

            // Parse the response
            if (lookupResponse?.Data?.Data == null)
            {
                _logger.LogWarning("TIN lookup returned null response for TIN: {Tin}", MaskTin(request.Tin));
                return TinValidationResult.Error("TIN validation service unavailable");
            }

            var lookupData = lookupResponse.Data.Data;

            // Check if buyer is enrolled (up == true means enrolled)
            if (lookupData.IsUp)
            {
                _logger.LogInformation(
                    "TIN {Tin} is valid and enrolled. Business: {BusinessRef}", 
                    MaskTin(request.Tin), 
                    lookupData.BusinessReference ?? "N/A");

                return TinValidationResult.ValidAndEnrolled(
                    businessName: lookupData.BusinessReference ?? "Unknown",
                    businessReference: lookupData.BusinessReference,
                    appReference: lookupData.AppReference,
                    hasWebhookSetup: lookupData.HasWebhookSetup);
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
