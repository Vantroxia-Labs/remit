using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetBroadcastById;

public record GetBroadcastByIdQuery(Guid Id) : IRequest<InvoiceBroadcastDto?>;
