using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Request;

public class AddressRequest
{
    /// <summary>
    /// Street address
    /// </summary>
    [Required(ErrorMessage = "Street address is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Street must be between 5 and 200 characters")]
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// City name
    /// </summary>
    [Required(ErrorMessage = "City is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province name
    /// </summary>
    [Required(ErrorMessage = "State is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "State must be between 2 and 100 characters")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    [Required(ErrorMessage = "Country is required")]
    [StringLength(2, ErrorMessage = "Country must be follow ISO standards(2 Characters)")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Postal or zip code (optional)
    /// </summary>
    [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string PostalCode { get; set; } = string.Empty;
}

public class SubscriptionRequest : IValidatableObject
{
    public Guid PlatformSubscriptionId { get; set; }

    [Range(3, 24, ErrorMessage = "Duration must be at least 3 months")]
    public int Duration { get; set; } = 3;

    [Required(ErrorMessage = "Subscription start date is required")]
    public DateOnly SubscriptionStartDate { get; set; }

    // Custom validation method
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (SubscriptionStartDate < today)
        {
            yield return new ValidationResult(
                "Subscription start date cannot be in the past",
                [nameof(SubscriptionStartDate)]);
        }
    }
}