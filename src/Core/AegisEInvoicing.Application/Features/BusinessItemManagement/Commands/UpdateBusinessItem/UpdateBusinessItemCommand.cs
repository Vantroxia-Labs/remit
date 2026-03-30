using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.UpdateBusinessItem;

public record UpdateBusinessItemCommand(
    Guid Id,
    string Name,
    UpdateServiceCodeDto ServiceCode,
    UpdateTaxCategoryDto TaxCategory,
    Guid ItemCategoryId,
    string ItemDescription,
    decimal UnitPrice) : IRequest<BusinessItemResult>, ITransactionalCommand;