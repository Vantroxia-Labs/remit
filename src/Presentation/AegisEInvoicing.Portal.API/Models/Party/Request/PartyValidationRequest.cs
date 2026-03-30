using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Party.Request;

public class PartyValidationRequest
{
    /// <summary>
    /// Dictionary of validation fields where key is the validation type and value is the field value to validate
    /// Supported validation types: ServiceId, BusinessRegistrationNumber, TaxIdentificationNumber
    /// </summary>
    [Required]
    public Dictionary<string, string> ValidationFields { get; set; } = new();
}
