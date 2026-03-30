namespace AegisEInvoicing.Portal.API.Models.Business.Response;

/// <summary>
/// Response model for business validation results
/// </summary>
public class BusinessValidationResponse
{
    /// <summary>
    /// Dictionary containing validation results where key is the validation type and value indicates if the field exists
    /// </summary>
    public Dictionary<string, bool> ValidationResults { get; set; } = new();

    /// <summary>
    /// Overall validation summary message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
