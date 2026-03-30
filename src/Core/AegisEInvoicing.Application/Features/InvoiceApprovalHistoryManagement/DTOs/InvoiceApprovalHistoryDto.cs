using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.InvoiceApprovalHistoryManagement.DTOs;

public record InvoiceApprovalHistoryResult(
bool IsSuccess,
string Message,
Guid? InvoiceApprovalHistoryId = null);

public record InvoiceApprovalHistoryDto(
    Guid Id,
    string IRN,
    InvoiceStatus InvoiceStatus,
    string CreatedByUserName,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    InvoiceStatus CurrentInvoiceStatus,
    string? Comments,
    string? FIRSSubmissionId);
