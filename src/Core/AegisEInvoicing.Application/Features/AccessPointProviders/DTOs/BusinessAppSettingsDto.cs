using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.AccessPointProviders.DTOs;

/// <summary>
/// The current APP provider selection and environment mode for a business.
/// Returned to the Settings page so the UI can reflect the current state.
/// </summary>
public record BusinessAppSettingsDto(
    AppVendor? ActiveVendor,
    AppEnvironmentMode EnvironmentMode);
