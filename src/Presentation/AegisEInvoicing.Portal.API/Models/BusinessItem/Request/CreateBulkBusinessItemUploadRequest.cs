using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.BusinessItem.Request;

public class CreateBulkBusinessItemUploadRequest
{

    [Required]
    public IFormFile file { get; set; } = null!;
}
