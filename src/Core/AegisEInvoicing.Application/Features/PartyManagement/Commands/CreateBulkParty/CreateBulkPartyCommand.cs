using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.PartyManagement.DTOs;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace AegisEInvoicing.Application.Features.PartyManagement.Commands.CreateBulkParty;

public record CreateBulkPartyCommand(IFormFile file) : IRequest<BulkPartyResult>, ITransactionalCommand;
