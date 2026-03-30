using EInvoiceIntegrator.Domain.Entities;
using EInvoiceIntegrator.Domain.ValueObjects;

namespace EInvoiceIntegrator.Application.Services;

public interface IDigitalSignatureService
{
    Task<SigningResult> SignInvoiceAsync(Invoice invoice, string ublDocument, CancellationToken cancellationToken = default);
    Task<bool> VerifySignatureAsync(DigitalSignature signature, string document, CancellationToken cancellationToken = default);
    Task<List<DigitalCertificate>> GetAvailableCertificatesAsync(Guid businessId, CancellationToken cancellationToken = default);
}

public class SigningResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public DigitalSignature? Signature { get; set; }
    public string? SignedDocument { get; set; }

    public static SigningResult Success(DigitalSignature signature, string signedDocument) => 
        new() { IsSuccess = true, Message = "Document signed successfully", Signature = signature, SignedDocument = signedDocument };

    public static SigningResult Failure(string message) => 
        new() { IsSuccess = false, Message = message };
}