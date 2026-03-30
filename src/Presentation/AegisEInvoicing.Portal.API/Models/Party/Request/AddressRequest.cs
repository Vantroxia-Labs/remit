using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Party.Request;

/// <summary>
/// Request model for address information
/// </summary>
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
    public string? PostalCode { get; set; }
}