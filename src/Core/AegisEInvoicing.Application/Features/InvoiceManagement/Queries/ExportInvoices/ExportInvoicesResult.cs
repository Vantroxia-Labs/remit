using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.ExportInvoices;

public record ExportInvoicesResult : GenericResult
{
    public byte[]? FileContents { get; init; }
    public string? FileName { get; init; }
    public string? ContentType { get; init; }
    public int TotalInvoices { get; init; }
    public int TotalItems { get; init; }
}
