using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.UpdateBusinessItem;

public record UpdateBusinessItemCommand(
    Guid Id,
    string Name,
    ItemType ItemType,
    UpdateServiceCodeDto ServiceCode,
    IEnumerable<UpdateBusinessItemTaxCategoryDto> TaxCategories,
    string ItemDescription,
    decimal UnitPrice) : IRequest<BusinessItemResult>, ITransactionalCommand;