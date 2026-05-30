namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

/// <summary>
/// Represents a tax category applied to a business item.
/// Supports both percentage-based and flat-fee tax types.
/// </summary>
public class BusinessItemTaxCategory
{
    /// <summary>FIRS tax category code (e.g. "STANDARD_VAT")</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Display name (e.g. "Standard Value-Added Tax")</summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// True = percentage tax (use Percent).
    /// False = flat fee (use FlatAmount).
    /// </summary>
    public bool IsPercentage { get; private set; }

    /// <summary>Tax rate 0–100. Set only when IsPercentage = true.</summary>
    public decimal? Percent { get; private set; }

    /// <summary>Fixed fee amount. Set only when IsPercentage = false.</summary>
    public decimal? FlatAmount { get; private set; }

    private BusinessItemTaxCategory() { } // Required for EF Core

    public static BusinessItemTaxCategory CreatePercentage(string code, string name, decimal percent)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tax category code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tax category name cannot be empty.", nameof(name));
        if (percent < 0 || percent > 100)
            throw new ArgumentOutOfRangeException(nameof(percent), "Tax percent must be between 0 and 100.");

        return new BusinessItemTaxCategory
        {
            Code = code.Trim(),
            Name = name.Trim(),
            IsPercentage = true,
            Percent = percent
        };
    }

    public static BusinessItemTaxCategory CreateFlatFee(string code, string name, decimal flatAmount)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Tax category code cannot be empty.", nameof(code));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tax category name cannot be empty.", nameof(name));
        if (flatAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(flatAmount), "Flat fee amount cannot be negative.");

        return new BusinessItemTaxCategory
        {
            Code = code.Trim(),
            Name = name.Trim(),
            IsPercentage = false,
            FlatAmount = flatAmount
        };
    }

    /// <summary>
    /// Calculates the tax amount for a given taxable amount.
    /// For percentage taxes: taxableAmount * (Percent / 100).
    /// For flat fees: FlatAmount (regardless of taxable amount).
    /// </summary>
    public decimal CalculateTax(decimal taxableAmount)
    {
        if (IsPercentage)
            return taxableAmount * (Percent!.Value / 100m);
        return FlatAmount!.Value;
    }
}
