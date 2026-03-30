namespace AegisEInvoicing.Portal.API.Models.Business.Request;

public record UpdateFirsCredentialsRequest(
    string FirsApiKey = null!,
    string FirsClientSecret = null!);