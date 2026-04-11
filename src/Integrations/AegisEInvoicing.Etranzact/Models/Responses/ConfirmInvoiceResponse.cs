using AegisEInvoicing.Etranzact.Models;

namespace AegisEInvoicing.Etranzact.Models.Responses;

/// <summary>
/// Response from the confirm invoice endpoint.
/// GET /api/v1/app/invoice/confirm/{irn}
/// </summary>
public sealed class ConfirmInvoiceResponse : EtranzactResponse<object?>
{
}
