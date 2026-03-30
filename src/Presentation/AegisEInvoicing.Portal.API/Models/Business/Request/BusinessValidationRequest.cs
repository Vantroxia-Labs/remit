using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Business.Request;

/// <summary>
/// Request model for validating business fields
/// </summary>
public class BusinessValidationRequest
{
    /// <summary>
    /// Dictionary of validation fields where key is the validation type and value is the field value to validate
    /// Supported validation types: ServiceId, BusinessRegistrationNumber, TaxIdentificationNumber
    /// </summary>
    [Required]
    public Dictionary<string, string> ValidationFields { get; set; } = new();
}
