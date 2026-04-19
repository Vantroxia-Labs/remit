using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.WhtScheduleManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.WhtScheduleManagement.Commands.MarkWhtScheduleFiled;

/// <summary>Marks an existing WHT schedule as filed with the tax authority.</summary>
public record MarkWhtScheduleFiledCommand(Guid ScheduleId) : IRequest<WhtScheduleDto>, ITransactionalCommand;
