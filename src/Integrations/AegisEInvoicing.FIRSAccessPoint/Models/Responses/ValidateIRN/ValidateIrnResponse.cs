namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.ValidateIRN;

public sealed record ValidateIrnResponse : GenericResponse
{
    public bool IsValid { get; set; }
    public string IrnStatus { get; set; } = null!;
}