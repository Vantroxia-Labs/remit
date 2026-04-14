namespace AegisEInvoicing.Portal.API.Models.Business.Sftp;

public class ChangeSftpPasswordWithOtpRequest
{
    public string Otp { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
