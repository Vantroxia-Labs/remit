using System.Text;
using System.Text.RegularExpressions;

namespace AegisEInvoicing.Application.Common.Security;

/// <summary>
/// Service for sanitizing user input to prevent XSS, SQL injection, and other injection attacks
/// Addresses VAPT finding: Improper input validation
/// </summary>
public static class InputSanitizationService
{
    // Dangerous patterns that should be blocked
    private static readonly string[] DangerousPatterns = new[]
    {
        // Script injection patterns
        @"<script[^>]*>.*?</script>",
        @"javascript:",
        @"onerror\s*=",
        @"onload\s*=",
        @"onclick\s*=",
        @"onmouseover\s*=",
        @"onfocus\s*=",
        @"<iframe",
        @"<embed",
        @"<object",

        // SQL injection patterns
        @";\s*drop\s+table",
        @";\s*delete\s+from",
        @";\s*update\s+",
        @";\s*insert\s+into",
        @"union\s+select",
        @"exec\s*\(",
        @"execute\s*\(",
        @"xp_cmdshell",
        @"sp_executesql",

        // Command injection patterns
        @"\|\s*\w+",
        @"&&\s*\w+",
        @";\s*\w+\s*\|",

        // LDAP injection patterns
        @"\(\s*\|\s*\(",
        @"\)\s*\(\s*\|",

        // XML injection patterns
        @"<!\[CDATA\[",
        @"<!DOCTYPE",
        @"<!ENTITY"
    };

    // Compile regex patterns for performance
    private static readonly Regex[] DangerousRegexPatterns = DangerousPatterns
        .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled))
        .ToArray();

    /// <summary>
    /// Sanitizes text input by removing HTML tags, dangerous characters, and potential injection attacks
    /// </summary>
    public static string SanitizeText(string? input, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Trim and limit length first
        var sanitized = input.Trim();
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);

        // HTML encode dangerous characters
        sanitized = System.Net.WebUtility.HtmlEncode(sanitized);

        // Check for dangerous patterns
        foreach (var pattern in DangerousRegexPatterns)
        {
            if (pattern.IsMatch(sanitized))
            {
                throw new ArgumentException($"Input contains potentially malicious content: {input.Substring(0, Math.Min(50, input.Length))}...");
            }
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes alphanumeric input (allows letters, numbers, spaces, and basic punctuation)
    /// Suitable for names, addresses, descriptions
    /// </summary>
    public static string SanitizeAlphanumeric(string? input, int maxLength = 200, bool allowSpecialChars = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = input.Trim();
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);

        // Define allowed characters based on parameter
        var allowedPattern = allowSpecialChars
            ? @"[^a-zA-Z0-9\s\-_.,&()/]"  // Allow common business characters
            : @"[^a-zA-Z0-9\s\-_.]";       // Only alphanumeric and basic punctuation

        // Remove disallowed characters
        sanitized = Regex.Replace(sanitized, allowedPattern, string.Empty);

        // Check for dangerous patterns
        foreach (var pattern in DangerousRegexPatterns)
        {
            if (pattern.IsMatch(sanitized))
            {
                throw new ArgumentException($"Input contains potentially malicious content");
            }
        }

        // Normalize multiple spaces to single space
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        return sanitized.Trim();
    }

    /// <summary>
    /// Validates and sanitizes email addresses
    /// </summary>
    public static string SanitizeEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;

        var sanitized = email.Trim().ToLowerInvariant();

        // Remove dangerous characters from email
        sanitized = Regex.Replace(sanitized, @"[^a-z0-9@._\-+]", string.Empty);

        // Validate email format
        var emailPattern = @"^[a-z0-9._%+\-]+@[a-z0-9.\-]+\.[a-z]{2,}$";
        if (!Regex.IsMatch(sanitized, emailPattern))
        {
            throw new ArgumentException("Invalid email format");
        }

        return sanitized;
    }

    /// <summary>
    /// Validates and sanitizes phone numbers (allows digits, +, -, spaces, parentheses)
    /// </summary>
    public static string SanitizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var sanitized = phone.Trim();

        // Allow only digits, +, -, spaces, and parentheses
        sanitized = Regex.Replace(sanitized, @"[^0-9+\-\s()]", string.Empty);

        // Remove excessive spaces
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        // Validate reasonable phone length (7-20 characters)
        var digitsOnly = Regex.Replace(sanitized, @"[^0-9]", string.Empty);
        if (digitsOnly.Length < 7 || digitsOnly.Length > 20)
        {
            throw new ArgumentException("Invalid phone number format");
        }

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes numeric input (allows only digits and optional decimal point)
    /// </summary>
    public static string SanitizeNumeric(string? input, bool allowDecimal = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = input.Trim();

        // Allow only digits and optionally decimal point
        var pattern = allowDecimal ? @"[^0-9.]" : @"[^0-9]";
        sanitized = Regex.Replace(sanitized, pattern, string.Empty);

        // Ensure only one decimal point
        if (allowDecimal && sanitized.Count(c => c == '.') > 1)
        {
            throw new ArgumentException("Invalid numeric format: multiple decimal points");
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes street addresses
    /// </summary>
    public static string SanitizeAddress(string? address, int maxLength = 200)
    {
        return SanitizeAlphanumeric(address, maxLength, allowSpecialChars: true);
    }

    /// <summary>
    /// Sanitizes city/state names (only letters, spaces, hyphens)
    /// </summary>
    public static string SanitizeCityState(string? input, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = input.Trim();
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);

        // Allow only letters, spaces, hyphens, and apostrophes (for place names)
        sanitized = Regex.Replace(sanitized, @"[^a-zA-Z\s\-']", string.Empty);

        // Normalize multiple spaces
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes postal/zip codes (alphanumeric with spaces and hyphens)
    /// </summary>
    public static string SanitizePostalCode(string? postalCode, int maxLength = 20)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
            return string.Empty;

        var sanitized = postalCode.Trim().ToUpperInvariant();

        // Allow only alphanumeric, spaces, and hyphens
        sanitized = Regex.Replace(sanitized, @"[^A-Z0-9\s\-]", string.Empty);

        // Remove excessive spaces
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        return sanitized.Trim();
    }

    /// <summary>
    /// Sanitizes TIN (Tax Identification Number) - allows only digits and hyphens
    /// </summary>
    public static string SanitizeTIN(string? tin)
    {
        if (string.IsNullOrWhiteSpace(tin))
            return string.Empty;

        var sanitized = tin.Trim();

        // Allow only digits and hyphens
        sanitized = Regex.Replace(sanitized, @"[^0-9\-]", string.Empty);

        // Validate format: should be 12 digits with optional hyphen
        var digitsOnly = sanitized.Replace("-", string.Empty);
        if (digitsOnly.Length != 12 || !digitsOnly.All(char.IsDigit))
        {
            throw new ArgumentException("Invalid TIN format: must be 12 digits");
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes country codes (2-letter ISO codes)
    /// </summary>
    public static string SanitizeCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
            return string.Empty;

        var sanitized = countryCode.Trim().ToUpperInvariant();

        // Allow only 2 letters
        if (!Regex.IsMatch(sanitized, @"^[A-Z]{2}$"))
        {
            throw new ArgumentException("Invalid country code: must be 2 uppercase letters (ISO 3166-1 alpha-2)");
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes currency codes (3-letter ISO codes)
    /// </summary>
    public static string SanitizeCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            return string.Empty;

        var sanitized = currencyCode.Trim().ToUpperInvariant();

        // Allow only 3 letters
        if (!Regex.IsMatch(sanitized, @"^[A-Z]{3}$"))
        {
            throw new ArgumentException("Invalid currency code: must be 3 uppercase letters (ISO 4217)");
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes search terms for use in database queries (LINQ Contains, etc.)
    /// Prevents SQL injection patterns and limits length to prevent DoS
    /// Addresses VAPT finding: SQL injection may be possible via query string
    /// </summary>
    public static string SanitizeSearchTerm(string? searchTerm, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return string.Empty;

        var sanitized = searchTerm.Trim();

        // Limit length to prevent DoS via extremely long search strings
        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);

        // Remove SQL-specific dangerous characters that could be used in injection
        // This includes: single quotes, double quotes, semicolons, comment markers
        sanitized = sanitized
            .Replace("'", string.Empty)
            .Replace("\"", string.Empty)
            .Replace(";", string.Empty)
            .Replace("--", string.Empty)
            .Replace("/*", string.Empty)
            .Replace("*/", string.Empty)
            .Replace("xp_", string.Empty)
            .Replace("sp_", string.Empty);

        // Check for dangerous patterns and throw if found
        foreach (var pattern in DangerousRegexPatterns)
        {
            if (pattern.IsMatch(sanitized))
            {
                throw new ArgumentException("Search term contains potentially malicious content");
            }
        }

        // Normalize multiple spaces to single space
        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        return sanitized.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Checks if input contains any dangerous patterns without sanitizing
    /// Returns true if input is safe, false if dangerous
    /// </summary>
    public static bool IsSafeInput(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return true;

        // Check for null bytes
        if (input.Contains("\0"))
            return false;

        // Check against all dangerous patterns
        foreach (var pattern in DangerousRegexPatterns)
        {
            if (pattern.IsMatch(input))
                return false;
        }

        return true;
    }
}
