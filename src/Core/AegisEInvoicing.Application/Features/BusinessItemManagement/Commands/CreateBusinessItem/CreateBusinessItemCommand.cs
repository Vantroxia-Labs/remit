using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBusinessItem;

public record CreateBusinessItemCommand(
    string Name,
    ItemType ItemType,
    CreateServiceCodeDto ServiceCode,
    IEnumerable<CreateBusinessItemTaxCategoryDto> TaxCategories,
    Guid ItemCategoryId,
    string ItemDescription,
    decimal UnitPrice) : IRequest<BusinessItemResult>, ITransactionalCommand;