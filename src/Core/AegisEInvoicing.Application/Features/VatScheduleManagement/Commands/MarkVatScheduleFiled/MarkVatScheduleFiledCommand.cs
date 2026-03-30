using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.VatScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VatScheduleManagement.Commands.MarkVatScheduleFiled;

/// <summary>Marks an existing VAT schedule as filed with FIRS.</summary>
public record MarkVatScheduleFiledCommand(Guid ScheduleId) : IRequest<VatScheduleDto>, ITransactionalCommand;
