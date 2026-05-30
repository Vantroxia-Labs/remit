namespace AegisEInvoicing.Application.Features.VendorManagement.DTOs;

public record VendorPortalFormDto(
    string BroadcastTitle,
    DateOnly DueDate,
    string InvoiceTypeCode,
    string Currency,
    bool RequiresApproval,
    string? Note,
    string TenantName,
    string VendorBusinessName,
    string VendorEmail,
    bool IsClosed);

public record VendorPortalVerifyResult(
    bool IsSuccess,
    string Message,
    string? VendorBusinessName = null,
    string? VendorEmail = null,
    string? VendorPhone = null);

public record VendorPortalLineItemDto(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    string? UnitOfMeasure = null);

public record VendorPortalSaveDraftDto(
    List<VendorPortalLineItemDto> LineItems,
    string? Note);

public record VendorPortalSubmitDto(
    List<VendorPortalLineItemDto> LineItems,
    string? Note);

public record VendorPortalCommandResult(bool IsSuccess, string Message);
