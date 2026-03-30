using AegisEInvoicing.Application.Common.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Commands.SyncReceivedInvoices;

/// <summary>
/// Command to synchronize received invoices from Interswitch for a specific business
/// </summary>
public sealed record SyncReceivedInvoicesCommand : IRequest<SyncReceivedInvoicesResult>, ITransactionalCommand
{
    /// <summary>
    /// Business ID to sync invoices for
    /// </summary>
    public Guid BusinessId { get; init; }

    /// <summary>
    /// Start date for invoice query (Format: yyyy-MM-dd)
    /// </summary>
    public string StartDate { get; init; } = string.Empty;

    /// <summary>
    /// End date for invoice query (Format: yyyy-MM-dd)
    /// </summary>
    public string EndDate { get; init; } = string.Empty;
}

/// <summary>
/// Result of synchronizing received invoices
/// </summary>
public sealed record SyncReceivedInvoicesResult
{
    /// <summary>
    /// Indicates if the sync was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Message describing the result
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Number of invoices synced (new invoices created)
    /// </summary>
    public int InvoicesSynced { get; init; }

    /// <summary>
    /// Number of new invoices created
    /// </summary>
    public int InvoicesCreated { get; init; }

    /// <summary>
    /// Number of invoices skipped (already existed in database)
    /// </summary>
    public int InvoicesSkipped { get; init; }

    /// <summary>
    /// Number of invoices updated (always 0 - we only create, never update)
    /// </summary>
    public int InvoicesUpdated { get; init; }

    /// <summary>
    /// List of any errors encountered
    /// </summary>
    public List<string> Errors { get; init; } = new();
}
