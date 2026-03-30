using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Commands.ProcessInvoiceTransmission;

/// <summary>
/// Command to process invoice transmission from queue
/// </summary>
public class ProcessInvoiceTransmissionCommand : IRequest<ProcessInvoiceTransmissionResult>
{
    /// <summary>
    /// Invoice Reference Number (IRN)
    /// </summary>
    public string Irn { get; set; } = string.Empty;

    /// <summary>
    /// Transmission status message
    /// </summary>
    public InvoiceStatus Status { get; set; }

    /// <summary>
    /// Additional metadata for the transmission
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Business ID for context
    /// </summary>
    public Guid? BusinessId { get; set; }

    /// <summary>
    /// User ID for context
    /// </summary>
    public Guid? UserId { get; set; }
}

/// <summary>
/// Result of invoice transmission processing
/// </summary>
public class ProcessInvoiceTransmissionResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
}