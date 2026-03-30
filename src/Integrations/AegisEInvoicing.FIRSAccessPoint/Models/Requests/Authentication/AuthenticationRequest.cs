using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.FIRSAccessPoint.Models.Requests.Authentication;

public sealed record AuthenticationRequest
{
    [EmailAddress]
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;
}