namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ImportFirsInvoices;

public record ImportFirsInvoicesResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int TotalFetched { get; init; }
    public int TotalImported { get; init; }
    public int TotalSkipped { get; init; }
    public int TotalFailed { get; init; }
    public List<string> ImportedIRNs { get; init; } = [];
    public List<string> SkippedIRNs { get; init; } = [];
    public List<string> FailedIRNs { get; init; } = [];
    public List<string> Errors { get; init; } = [];
}
