using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceManagement.Commands.CreateAndSubmitInvoice;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.SubmitExistingInvoice;

/// <summary>
/// Command to submit an existing invoice through the pipeline:
/// Validate ? Sign ? Transmit
/// </summary>
public record SubmitExistingInvoiceCommand : IRequest<CreateAndSubmitInvoiceResult>, ITransactionalCommand
{
    /// <summary>
    /// The ID of the existing invoice to submit
    /// </summary>
    public Guid InvoiceId { get; init; }
}
