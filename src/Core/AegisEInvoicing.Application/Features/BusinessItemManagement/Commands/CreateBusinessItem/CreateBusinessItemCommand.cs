using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBusinessItem;

public record CreateBusinessItemCommand(
    string Name,
    CreateServiceCodeDto ServiceCode,
    CreateTaxCategoryDto TaxCategory,
    Guid ItemCategoryId,
    string ItemDescription,
    decimal UnitPrice) : IRequest<BusinessItemResult>, ITransactionalCommand;