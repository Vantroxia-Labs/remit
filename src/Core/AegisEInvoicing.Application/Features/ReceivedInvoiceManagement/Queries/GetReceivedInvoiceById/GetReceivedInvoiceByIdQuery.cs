using AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.ReceivedInvoiceManagement.Queries.GetReceivedInvoiceById;

/// <summary>
/// Query to retrieve a single received invoice by ID with full details
/// </summary>
public sealed record GetReceivedInvoiceByIdQuery : IRequest<GetReceivedInvoiceByIdResult>
{
    /// <summary>
    /// Invoice ID to retrieve
    /// </summary>
    public Guid InvoiceId { get; init; }

    /// <summary>
    /// Business ID for authorization (optional - defaults to current user's business)
    /// </summary>
    public Guid? BusinessId { get; init; }
}

/// <summary>
/// Result containing a single received invoice with full details
/// </summary>
public sealed record GetReceivedInvoiceByIdResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public ReceivedInvoiceDetailDto? Invoice { get; init; }
}
