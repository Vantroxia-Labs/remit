using FluentValidation;

namespace AegisEInvoicing.Application.Common.Security;

/// <summary>
/// FluentValidation extensions for input sanitization and security validation
/// Addresses VAPT finding: Improper input validation
/// </summary>
public static class InputValidationExtensions
{
    /// <summary>
    /// Validates that the string contains only safe characters (no XSS, SQL injection, etc.)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeInput<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(input => InputSanitizationService.IsSafeInput(input))
            .WithMessage("Input contains potentially malicious content. Only alphanumeric characters and basic punctuation are allowed.");
    }

    /// <summary>
    /// Validates that the string contains only alphanumeric characters and basic punctuation
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeAlphanumeric<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maxLength = 200,
        bool allowSpecialChars = false)
    {
        return ruleBuilder
            .Must(input =>
            {
                if (string.IsNullOrWhiteSpace(input))
                    return true;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeAlphanumeric(input, maxLength, allowSpecialChars);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage(allowSpecialChars
                ? $"Field must contain only letters, numbers, spaces, and basic business characters (maximum {maxLength} characters)"
                : $"Field must contain only letters, numbers, spaces, hyphens, underscores, and periods (maximum {maxLength} characters)");
    }

    /// <summary>
    /// Validates email format and security
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeEmail<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(email =>
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeEmail(email);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Invalid email format. Only standard email characters are allowed.");
    }

    /// <summary>
    /// Validates phone number format and security
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafePhone<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(phone =>
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizePhone(phone);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Invalid phone number format. Only digits, +, -, spaces, and parentheses are allowed (7-20 digits).");
    }

    /// <summary>
    /// Validates street address format and security
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeAddress<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maxLength = 200)
    {
        return ruleBuilder
            .Must(address =>
            {
                if (string.IsNullOrWhiteSpace(address))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeAddress(address, maxLength);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage($"Invalid address format. Only letters, numbers, spaces, and basic punctuation are allowed (maximum {maxLength} characters).");
    }

    /// <summary>
    /// Validates city/state name format and security
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeCityState<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maxLength = 100)
    {
        return ruleBuilder
            .Must(input =>
            {
                if (string.IsNullOrWhiteSpace(input))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeCityState(input, maxLength);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage($"Invalid city/state format. Only letters, spaces, hyphens, and apostrophes are allowed (maximum {maxLength} characters).");
    }

    /// <summary>
    /// Validates postal code format and security
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafePostalCode<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maxLength = 20)
    {
        return ruleBuilder
            .Must(input =>
            {
                if (string.IsNullOrWhiteSpace(input))
                    return true; // Postal code is often optional

                try
                {
                    var sanitized = InputSanitizationService.SanitizePostalCode(input, maxLength);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage($"Invalid postal code format. Only alphanumeric characters, spaces, and hyphens are allowed (maximum {maxLength} characters).");
    }

    /// <summary>
    /// Validates that numeric input contains only digits
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeNumeric<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        bool allowDecimal = false)
    {
        return ruleBuilder
            .Must(input =>
            {
                if (string.IsNullOrWhiteSpace(input))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeNumeric(input, allowDecimal);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage(allowDecimal
                ? "Field must contain only digits and an optional decimal point"
                : "Field must contain only digits");
    }

    /// <summary>
    /// Validates that text input is safe and within length limits
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeText<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        int maxLength = 500)
    {
        return ruleBuilder
            .Must(input =>
            {
                if (string.IsNullOrWhiteSpace(input))
                    return true;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeText(input, maxLength);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage($"Input contains invalid or potentially malicious content (maximum {maxLength} characters)");
    }

    /// <summary>
    /// Validates TIN format (12 digits with optional hyphen)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeTIN<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(tin =>
            {
                if (string.IsNullOrWhiteSpace(tin))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeTIN(tin);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Invalid TIN format. Must be 12 digits with optional hyphen separator.");
    }

    /// <summary>
    /// Validates country code (2-letter ISO code)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeCountryCode<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(code =>
            {
                if (string.IsNullOrWhiteSpace(code))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeCountryCode(code);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Invalid country code. Must be 2 uppercase letters (ISO 3166-1 alpha-2).");
    }

    /// <summary>
    /// Validates currency code (3-letter ISO code)
    /// </summary>
    public static IRuleBuilderOptions<T, string?> MustBeSafeCurrencyCode<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(code =>
            {
                if (string.IsNullOrWhiteSpace(code))
                    return false;

                try
                {
                    var sanitized = InputSanitizationService.SanitizeCurrencyCode(code);
                    return !string.IsNullOrEmpty(sanitized);
                }
                catch
                {
                    return false;
                }
            })
            .WithMessage("Invalid currency code. Must be 3 uppercase letters (ISO 4217).");
    }
}
