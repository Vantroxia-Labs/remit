using AegisEInvoicing.Application.Features.VendorManagement.DTOs;
using AegisEInvoicing.Application.Interfaces;
using MediatR;

namespace AegisEInvoicing.Application.Features.VendorManagement.Queries.GetVendorPortalForm;

public record GetVendorPortalFormQuery(string Token) : IRequest<VendorPortalFormDto?>;
