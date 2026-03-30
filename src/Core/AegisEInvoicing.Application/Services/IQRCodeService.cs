using EInvoiceIntegrator.Domain.ValueObjects;

namespace EInvoiceIntegrator.Application.Services;

public interface IQRCodeService
{
    Task<QRCodeResult> GenerateQRCodeAsync(string invoiceReferenceNumber, DateTime timestamp, CancellationToken cancellationToken = default);
    Task<QRCodeValidationResult> ValidateQRCodeAsync(string encryptedData, CancellationToken cancellationToken = default);
}

public class QRCodeResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public QRCode? QRCode { get; set; }

    public static QRCodeResult Success(QRCode qrCode) => 
        new() { IsSuccess = true, Message = "QR code generated successfully", QRCode = qrCode };

    public static QRCodeResult Failure(string message) => 
        new() { IsSuccess = false, Message = message };
}

public class QRCodeValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? InvoiceReferenceNumber { get; set; }
    public DateTime? Timestamp { get; set; }

    public static QRCodeValidationResult Valid(string irn, DateTime timestamp) => 
        new() { IsValid = true, Message = "QR code is valid", InvoiceReferenceNumber = irn, Timestamp = timestamp };

    public static QRCodeValidationResult Invalid(string message) => 
        new() { IsValid = false, Message = message };
}