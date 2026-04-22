using AegisEInvoicing.Portal.API.Models.BusinessOnboarding.Request;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Business.Request
{
    public class UpdateBusinessRequest
    {
        [Required]
        public AddressRequest RegisteredAddress { get; set; } = new();

        [Required]
        public string Industry { get; set; } = string.Empty;

        public string? InvoicePrefix { get; set; }

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        [Required]
        public string ContactPhone { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>FIRS-assigned 8-character service ID for IRN generation.</summary>
        public string? ServiceId { get; set; }

        /// <summary>CAC or relevant business registration number.</summary>
        public string? BusinessRegistrationNumber { get; set; }

        /// <summary>Tax Identification Number (TIN).</summary>
        public string? TaxIdentificationNumber { get; set; }

        /// <summary>NRS / FIRS-assigned Business ID (GUID string).</summary>
        public string? NRSBusinessId { get; set; }
    }
}
