namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllLocalGovernments;

public sealed record GetAllLocalGovernmentsResponse : GenericResponse
{
    public List<LocalGovernment> Data { get; set; } = new();
}

public sealed record LocalGovernment
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string StateId { get; set; } = null!;
}