using System.ComponentModel.DataAnnotations;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Portal.API.Models.AccessPointProvider;

public class SetBusinessEnvironmentModeRequest
{
    [Required]
    public AppEnvironmentMode EnvironmentMode { get; set; }
}
