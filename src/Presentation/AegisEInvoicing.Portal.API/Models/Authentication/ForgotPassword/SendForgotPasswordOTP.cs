using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Authentication.ForgotPassword;

public class SendForgotPasswordOTP
{
    [Required]
    public string PhoneNo_Email { get; set; } = null!;
}
