using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceManagement.Queries.GetVatRemittanceReport;

public record GetVatRemittanceReportQuery(
    DateOnly StartDate,
    DateOnly EndDate) : IRequest<VatRemittanceReportDto>;
