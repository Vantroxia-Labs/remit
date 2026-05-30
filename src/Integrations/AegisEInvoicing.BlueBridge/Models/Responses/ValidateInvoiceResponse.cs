using AegisEInvoicing.BlueBridge.Models;

namespace AegisEInvoicing.BlueBridge.Models.Responses;

/// <summary>
/// Response from Validate Invoice endpoint.
/// POST /api/v1/invoices/validate
/// </summary>
public sealed class ValidateInvoiceResponse : BlueBridgeResponse<object?>
{
}
