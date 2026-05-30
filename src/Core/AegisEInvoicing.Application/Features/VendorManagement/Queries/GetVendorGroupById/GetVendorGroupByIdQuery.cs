using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorGroupById;

public record GetVendorGroupByIdQuery(Guid Id) : IRequest<VendorGroupDto?>;
