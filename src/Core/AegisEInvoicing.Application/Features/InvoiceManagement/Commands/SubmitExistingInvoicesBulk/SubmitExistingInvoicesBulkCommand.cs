using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitBulkInvoice;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoicesBulk;

/// <summary>
/// Command to submit multiple existing invoices through the pipeline in bulk
/// </summary>
public record SubmitExistingInvoicesBulkCommand : IRequest<CreateAndSubmitBulkInvoiceResult>, ITransactionalCommand
{
    /// <summary>
    /// List of invoice IDs to submit
    /// </summary>
    public List<Guid> InvoiceIds { get; init; } = [];
}
