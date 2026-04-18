using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorById;

public record GetVendorByIdQuery(Guid Id) : IRequest<VendorDto?>;
