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

        [Required]
        public string InvoicePrefix { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string ContactEmail { get; set; } = string.Empty;

        [Required]
        public string ContactPhone { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;
    }
}
