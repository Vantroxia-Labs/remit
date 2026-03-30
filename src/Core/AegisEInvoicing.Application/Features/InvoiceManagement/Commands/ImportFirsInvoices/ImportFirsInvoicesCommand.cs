using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ImportFirsInvoices;

public record ImportFirsInvoicesCommand : IRequest<ImportFirsInvoicesResult>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
