using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Application.Features.ItemCategoryManagement.DTOs;
using MediatR;

namespace AegisEInvoicing.Application.Features.ItemCategoryManagement.Commands.UpdateItemCategory;

public record UpdateItemCategoryCommand(
    Guid Id,
    string Name,
    string Description) : IRequest<ItemCategoryResult>, ITransactionalCommand;