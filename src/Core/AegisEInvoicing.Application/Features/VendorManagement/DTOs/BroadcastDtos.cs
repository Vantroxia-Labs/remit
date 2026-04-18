using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.VendorManagement.DTOs;

public record InvoiceBroadcastSummaryDto(
    Guid Id,
    string Title,
    string InvoiceTypeCode,
    DateOnly DueDate,
    bool RequiresApproval,
    bool IsApprovalLocked,
    BroadcastStatus Status,
    string Currency,
    string? Note,
    int TotalVendors,
    int SubmittedCount,
    DateTimeOffset CreatedAt);

public record InvoiceBroadcastDto(
    Guid Id,
    string Title,
    string InvoiceTypeCode,
    DateOnly DueDate,
    bool RequiresApproval,
    bool IsApprovalLocked,
    BroadcastStatus Status,
    string Currency,
    string? Note,
    Guid BusinessId,
    IReadOnlyList<BroadcastVendorDto> BroadcastVendors,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record BroadcastVendorDto(
    Guid Id,
    Guid VendorId,
    string VendorBusinessName,
    string VendorEmail,
    bool IsEmailVerified,
    Guid? InvoiceId,
    string? InvoiceStatus,
    string? InvoicePaymentStatus,
    DateTimeOffset? EmailVerifiedAt);

public record BroadcastVendorSubmissionDto(
    Guid Id,
    Guid VendorId,
    string VendorBusinessName,
    string VendorEmail,
    bool IsEmailVerified,
    Guid? InvoiceId,
    string? InvoiceCode,
    string? Irn,
    string? InvoiceStatus,
    string? InvoicePaymentStatus,
    DateTimeOffset? EmailVerifiedAt,
    DateTimeOffset? SubmittedAt);

public record InvoiceBroadcastResult(bool IsSuccess, string Message, Guid? Id = null);
public record DeactivateBroadcastResult(bool IsSuccess, string Message, bool HasPendingInvoices = false);
