namespace AegisEInvoicing.Portal.API.Models.Business.Request;

public record UpdateQrCodeConfigurationRequest(
    string PublicKey = null!,
    string Certificate = null!);
