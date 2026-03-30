using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessItemManagement.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace AegisEInvoicing.Application.Features.BusinessItemManagement.Commands.CreateBulkBusinessItem;

public record CreateBulkBusinessItemCommand(IFormFile file) : IRequest<BulkItemResult>, ITransactionalCommand;
