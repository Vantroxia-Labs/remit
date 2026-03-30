namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.ConfirmInvoice;

public sealed record ConfirmInvoiceResponse : GenericResponse
{
    public string ConfirmationId { get; set; } = null!;
}