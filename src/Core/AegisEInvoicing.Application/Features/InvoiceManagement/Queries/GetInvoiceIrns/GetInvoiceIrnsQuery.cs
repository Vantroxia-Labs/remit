using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetInvoiceIrns;

public record GetInvoiceIrnsQuery() : IRequest<GetInvoiceIrnsResult>;
