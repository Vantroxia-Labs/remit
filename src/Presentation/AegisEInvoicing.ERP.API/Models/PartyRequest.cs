using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.ERP.API.Models;

/// <summary>
/// Request model for party (customer or supplier) information
/// </summary>
public class PartyRequest
{
    /// <summary>
    /// Name of the party (individual or business)
    /// </summary>
    /// <example>ABC Manufacturing Ltd.</example>
    [Required(ErrorMessage = "Party name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description or business summary of the party
    /// </summary>
    /// <example>Registered supplier of industrial electrical materials and installation services</example>
    [Required(ErrorMessage = "Party description is required")]
    [StringLength(2000, MinimumLength = 2, ErrorMessage = "Description must be between 2 and 2000 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the party
    /// </summary>
    /// <example>+2348012345678</example>
    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^(?:\+234|234|0)\d{10}$",
      ErrorMessage = "Please enter a valid Nigerian phone number")]
    [StringLength(20, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 20 characters")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the party
    /// </summary>
    /// <example>finance@abcmfg.com</example>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number (TIN) of the party
    /// </summary>
    /// <example>12345678-0001</example>
    [Required(ErrorMessage = "Tax identification number is required")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Tax identification number must be between 5 and 50 characters")]
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Address information of the party
    /// </summary>
    /// <example>
    /// {
    ///   "street": "15 Industrial Layout",
    ///   "city": "Ikeja",
    ///   "state": "Lagos",
    ///   "country": "Nigeria",
    ///   "postalCode": "100271"
    /// }
    /// </example>
    [Required(ErrorMessage = "Address information is required")]
    public AddressRequest Address { get; set; } = new();
}

/// <summary>
/// Request model for address information
/// </summary>
public class AddressRequest
{
    /// <summary>
    /// Street address
    /// </summary>
    /// <example>15 Industrial Layout</example>
    [Required(ErrorMessage = "Street address is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Street must be between 5 and 200 characters")]
    public string Street { get; set; } = string.Empty;

    /// <summary>
    /// City name
    /// </summary>
    /// <example>Ikeja</example>
    [Required(ErrorMessage = "City is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "City must be between 2 and 100 characters")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// State or province name
    /// </summary>
    /// <example>Lagos</example>
    [Required(ErrorMessage = "State is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "State must be between 2 and 100 characters")]
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Country name
    /// </summary>
    /// <example>NG</example>
    [Required(ErrorMessage = "Country is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Country must be between 2 and 100 characters")]
    public string Country { get; set; } = string.Empty;

    /// <summary>
    /// Postal or zip code
    /// </summary>
    /// <example>100271</example>
    [StringLength(20, ErrorMessage = "Postal code cannot exceed 20 characters")]
    public string? PostalCode { get; set; }
}
