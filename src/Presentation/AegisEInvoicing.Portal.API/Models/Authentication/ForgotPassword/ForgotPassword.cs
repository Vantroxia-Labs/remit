using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Portal.API.Models.Authentication.ForgotPassword;

public class ForgotPassword
{
    [Required]
    public string Otp { get; set; } = null!;
    [Required]
    public string Password { get; set; } = null!;
    [Required]
    public string PhoneNo_Email { get; set; } = null!;
}
