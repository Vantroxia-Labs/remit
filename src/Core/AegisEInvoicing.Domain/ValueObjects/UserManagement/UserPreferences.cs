using AegisEInvoicing.Domain.Common.Implementation;

namespace AegisEInvoicing.Domain.ValueObjects.UserManagement;

/// <summary>
/// Represents user preferences and settings
/// </summary>
public class UserPreferences : ValueObject
{
    public string Language { get; }
    public string TimeZone { get; }
    public string DateFormat { get; }
    public string NumberFormat { get; }
    public bool EmailNotifications { get; }
    public bool SmsNotifications { get; }
    public bool InvoiceReminders { get; }
    public bool SecurityAlerts { get; }
    public string Theme { get; }
    public int PageSize { get; }
    public bool TwoFactorEnabled { get; }

    // Parameterless constructor for Entity Framework
    private UserPreferences()
    {
        Language = "en-US";
        TimeZone = "UTC";
        DateFormat = "yyyy-MM-dd";
        NumberFormat = "en-US";
        EmailNotifications = true;
        SmsNotifications = false;
        InvoiceReminders = true;
        SecurityAlerts = true;
        Theme = "light";
        PageSize = 20;
        TwoFactorEnabled = false;
    }

    private UserPreferences(
        string language,
        string timeZone,
        string dateFormat,
        string numberFormat,
        bool emailNotifications,
        bool smsNotifications,
        bool invoiceReminders,
        bool securityAlerts,
        string theme,
        int pageSize,
        bool twoFactorEnabled)
    {
        Language = language;
        TimeZone = timeZone;
        DateFormat = dateFormat;
        NumberFormat = numberFormat;
        EmailNotifications = emailNotifications;
        SmsNotifications = smsNotifications;
        InvoiceReminders = invoiceReminders;
        SecurityAlerts = securityAlerts;
        Theme = theme;
        PageSize = pageSize;
        TwoFactorEnabled = twoFactorEnabled;
    }

    public static UserPreferences Create(
        string language = "en-US",
        string timeZone = "UTC",
        string dateFormat = "yyyy-MM-dd",
        string numberFormat = "en-US",
        bool emailNotifications = true,
        bool smsNotifications = false,
        bool invoiceReminders = true,
        bool securityAlerts = true,
        string theme = "light",
        int pageSize = 25,
        bool twoFactorEnabled = false)
    {
        ValidateInputs(language, timeZone, dateFormat, numberFormat, theme, pageSize);

        return new UserPreferences(
            language,
            timeZone,
            dateFormat,
            numberFormat,
            emailNotifications,
            smsNotifications,
            invoiceReminders,
            securityAlerts,
            theme,
            pageSize,
            twoFactorEnabled);
    }

    public static UserPreferences Default()
    {
        return Create();
    }

    public UserPreferences UpdateLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language is required", nameof(language));

        return new UserPreferences(
            language,
            TimeZone,
            DateFormat,
            NumberFormat,
            EmailNotifications,
            SmsNotifications,
            InvoiceReminders,
            SecurityAlerts,
            Theme,
            PageSize,
            TwoFactorEnabled);
    }

    public UserPreferences UpdateTimeZone(string timeZone)
    {
        if (string.IsNullOrWhiteSpace(timeZone))
            throw new ArgumentException("TimeZone is required", nameof(timeZone));

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException("Invalid time zone", nameof(timeZone));
        }

        return new UserPreferences(
            Language,
            timeZone,
            DateFormat,
            NumberFormat,
            EmailNotifications,
            SmsNotifications,
            InvoiceReminders,
            SecurityAlerts,
            Theme,
            PageSize,
            TwoFactorEnabled);
    }

    public UserPreferences UpdateNotificationSettings(
        bool emailNotifications,
        bool smsNotifications,
        bool invoiceReminders,
        bool securityAlerts)
    {
        return new UserPreferences(
            Language,
            TimeZone,
            DateFormat,
            NumberFormat,
            emailNotifications,
            smsNotifications,
            invoiceReminders,
            securityAlerts,
            Theme,
            PageSize,
            TwoFactorEnabled);
    }

    public UserPreferences UpdateDisplaySettings(
        string theme,
        int pageSize,
        string dateFormat,
        string numberFormat)
    {
        ValidateDisplaySettings(theme, pageSize, dateFormat, numberFormat);

        return new UserPreferences(
            Language,
            TimeZone,
            dateFormat,
            numberFormat,
            EmailNotifications,
            SmsNotifications,
            InvoiceReminders,
            SecurityAlerts,
            theme,
            pageSize,
            TwoFactorEnabled);
    }

    public UserPreferences EnableTwoFactor()
    {
        return new UserPreferences(
            Language,
            TimeZone,
            DateFormat,
            NumberFormat,
            EmailNotifications,
            SmsNotifications,
            InvoiceReminders,
            SecurityAlerts,
            Theme,
            PageSize,
            true);
    }

    public UserPreferences DisableTwoFactor()
    {
        return new UserPreferences(
            Language,
            TimeZone,
            DateFormat,
            NumberFormat,
            EmailNotifications,
            SmsNotifications,
            InvoiceReminders,
            SecurityAlerts,
            Theme,
            PageSize,
            false);
    }

    private static void ValidateInputs(
        string language,
        string timeZone,
        string dateFormat,
        string numberFormat,
        string theme,
        int pageSize)
    {
        if (string.IsNullOrWhiteSpace(language))
            throw new ArgumentException("Language is required", nameof(language));

        if (string.IsNullOrWhiteSpace(timeZone))
            throw new ArgumentException("TimeZone is required", nameof(timeZone));

        if (string.IsNullOrWhiteSpace(dateFormat))
            throw new ArgumentException("DateFormat is required", nameof(dateFormat));

        if (string.IsNullOrWhiteSpace(numberFormat))
            throw new ArgumentException("NumberFormat is required", nameof(numberFormat));

        ValidateDisplaySettings(theme, pageSize, dateFormat, numberFormat);

        // Validate timezone
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            throw new ArgumentException("Invalid time zone", nameof(timeZone));
        }
    }

    private static void ValidateDisplaySettings(string theme, int pageSize, string dateFormat, string numberFormat)
    {
        if (string.IsNullOrWhiteSpace(theme))
            throw new ArgumentException("Theme is required", nameof(theme));

        var validThemes = new[] { "light", "dark", "auto" };
        if (!validThemes.Contains(theme.ToLowerInvariant()))
            throw new ArgumentException("Invalid theme. Supported themes: light, dark, auto", nameof(theme));

        if (pageSize <= 0 || pageSize > 100)
            throw new ArgumentException("PageSize must be between 1 and 100", nameof(pageSize));

        // Validate date format by trying to format current date
        try
        {
            DateTime.Now.ToString(dateFormat);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Invalid date format", nameof(dateFormat));
        }

        // Validate number format
        try
        {
            var culture = new System.Globalization.CultureInfo(numberFormat);
        }
        catch (ArgumentException)
        {
            // Fallback for Globalization Invariant Mode (common in minimal container environments)
            // If the value is a standard BCP-47 language tag (e.g. "en-US", "en-NG"), we allow it.
            var isStandardTag = System.Text.RegularExpressions.Regex.IsMatch(
                numberFormat,
                "^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,4})?$",
                System.Text.RegularExpressions.RegexOptions.None,
                TimeSpan.FromMilliseconds(100));

            if (!isStandardTag && !numberFormat.Equals("invariant", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Invalid number format culture", nameof(numberFormat));
            }
        }
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Language;
        yield return TimeZone;
        yield return DateFormat;
        yield return NumberFormat;
        yield return EmailNotifications;
        yield return SmsNotifications;
        yield return InvoiceReminders;
        yield return SecurityAlerts;
        yield return Theme;
        yield return PageSize;
        yield return TwoFactorEnabled;
    }

    public override string ToString()
    {
        return $"Language: {Language}, TimeZone: {TimeZone}, Theme: {Theme}";
    }
}