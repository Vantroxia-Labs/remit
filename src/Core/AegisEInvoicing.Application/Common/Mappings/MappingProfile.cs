using AegisEInvoicing.Application.Features.VatScheduleManagement.VatScheduleExport.Dto;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AutoMapper;

namespace AegisEInvoicing.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for application mappings
/// </summary>
public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<VatScheduleItem, VatScheduleItemExportDto>();
    }
}
