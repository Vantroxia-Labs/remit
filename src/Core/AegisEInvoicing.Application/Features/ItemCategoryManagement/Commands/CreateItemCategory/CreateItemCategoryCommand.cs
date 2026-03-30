using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.CreateItemCategory;

public record CreateItemCategoryCommand(
    string Name,
    string Description) : IRequest<ItemCategoryResult>, ITransactionalCommand;