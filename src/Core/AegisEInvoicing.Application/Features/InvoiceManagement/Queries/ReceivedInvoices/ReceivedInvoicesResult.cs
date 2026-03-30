using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Domain.Models;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.ReceivedInvoices;

public record ReceivedInvoicesResult
{
    public PaginatedList<ReceivedInvoicesDto> Invoices { get; init; } = null!;
}


public record ReceivedInvoicesDto
{
}
