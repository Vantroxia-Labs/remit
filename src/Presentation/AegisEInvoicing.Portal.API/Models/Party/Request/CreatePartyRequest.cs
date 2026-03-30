using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Party.Request;

/// <summary>
/// Request model for creating a new party
/// </summary>
public class CreatePartyRequest
{
    /// <summary>
    /// Name of the party (individual or business)
    /// </summary>
    [Required(ErrorMessage = "Party name is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Name of the party (individual or business)
    /// </summary>
    [Required(ErrorMessage = "Party description is required")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 200 characters")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the party
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(14, MinimumLength = 14, ErrorMessage = "Phone number must be exactly 14 characters")]
    [RegularExpression(@"^\+234[7-9]\d{9}$", ErrorMessage = "Please enter a valid Nigerian phone number (e.g., +2348102341892)")]
    public string Phone { get; set; } = string.Empty;

    /// <summary>
    /// Email address of the party
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(255, ErrorMessage = "Email address cannot exceed 255 characters")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number (TIN) of the party
    /// </summary>
    [Required(ErrorMessage = "Tax identification number is required")]
    [StringLength(50, MinimumLength = 5, ErrorMessage = "Tax identification number must be between 5 and 50 characters")]
    public string TaxIdentificationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Address information of the party
    /// </summary>
    [Required(ErrorMessage = "Address information is required")]
    public AddressRequest Address { get; set; } = new();
}