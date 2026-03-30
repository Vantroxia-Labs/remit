using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.DTOs;
using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.Commands.CreateInvoiceApprovalHistory;

public record CreateInvoiceApprovalHistoryCommand(
    Guid InvoiceId,
    string Comment,
    InvoiceStatus InvoiceStatus) : IRequest<InvoiceApprovalHistoryResult>, ITransactionalCommand;