using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Party.Request;

public class CreateBulkPartyUploadRequest
{
    [Required]
    public IFormFile file { get; set; } = null!;
}
