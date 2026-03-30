using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Business.Sftp;

public sealed class ToggleSftpTransmissionRequest
{
    [Required]
    public bool Enabled { get; set; }
}