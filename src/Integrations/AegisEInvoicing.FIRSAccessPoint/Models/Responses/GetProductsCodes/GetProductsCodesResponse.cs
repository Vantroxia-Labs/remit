namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetProductsCodes;

public sealed record GetProductsCodesResponse : GenericResponse
{
    public List<ProductCode> Data { get; set; } = new();
}

public sealed record ProductCode
{
    public string Code { get; set; } = null!;
    public string Description { get; set; } = null!;
}