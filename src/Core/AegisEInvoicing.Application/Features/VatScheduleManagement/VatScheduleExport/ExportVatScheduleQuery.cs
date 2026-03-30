using MediatR;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport
{
    public record ExportVatScheduleQuery(Guid Id) : IRequest<byte[]>;
}
