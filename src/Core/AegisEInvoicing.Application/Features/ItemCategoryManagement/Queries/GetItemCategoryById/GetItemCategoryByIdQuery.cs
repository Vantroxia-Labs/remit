using AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Queries.GetItemCategoryById;

public record GetItemCategoryByIdQuery(Guid Id) : IRequest<GetItemCategoryByIdResult>;