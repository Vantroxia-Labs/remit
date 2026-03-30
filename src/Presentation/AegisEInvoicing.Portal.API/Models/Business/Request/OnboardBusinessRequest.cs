using AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Request;
using AegisEInvoicing.Domain.Entities;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Business.Request;

public class OnboardBusinessRequest
{
    [Required]
    public string BusinessName { get; set; } = string.Empty;

    [Required]
    [StringLength(13, ErrorMessage = "Tax Identification Number is required and cannot be greater than 13 characters")]
    public string TIN { get; set; } = string.Empty;

    [Required]
    [StringLength(11, ErrorMessage = "Business Registration Number is required and cannot be greater than 11 characters")]
    public string BusinessRegistrationNumber { get; set; } = string.Empty;
    
    [Required]
    public AddressRequest RegisteredAddress { get; set; } = new();

    [Required]
    public Guid FIRSBusinessId { get; set; }

    [Required]
    public string Industry {  get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string ContactEmail { get; set; } = string.Empty;

    /// <summary>
    /// Phone number of the party
    /// </summary>
    [Required(ErrorMessage = "Phone number is required")]
    [StringLength(14, MinimumLength = 14, ErrorMessage = "Phone number must be exactly 14 characters")]
    [RegularExpression(@"^\+234[7-9]\d{9}$", ErrorMessage = "Please enter a valid Nigerian phone number (e.g., +2348102341892)")]
    public string ContactPhone { get; set; } = string.Empty;

    [Required]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required]
    [MaxLength(8)]
    public string ServiceId {  get; set; } = string.Empty;

    [Required]
    public string AdminFirstName { get; set; } = string.Empty;

    [Required]
    public string AdminLastName { get; set; } = string.Empty;

    [Required]
    public SubscriptionRequest Subscription { get; set; } = new();

    /// <summary>
    /// Optional: Deployment mode (OnPremise or SaaS). Defaults to SaaS if not specified.
    /// Only Aegis admins can set this field.
    /// </summary>
    public DeploymentMode? DeploymentMode { get; set; }
}
