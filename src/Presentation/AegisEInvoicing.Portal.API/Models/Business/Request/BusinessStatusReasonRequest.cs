namespace AegisEInvoicing.Portal.API.Models.Business.Request;

public record BusinessStatusReasonRequest
{
    public string? Reason { get; init; }
}
