namespace AegisEInvoicing.FIRSAccessPoint.Models.Responses.GetAllStates;

public sealed record GetAllStatesResponse : GenericResponse
{
    public List<State> Data { get; set; } = new();
}

public sealed record State
{
    public string Id { get; set; } = null!;
    public string Name { get; set; } = null!;
}