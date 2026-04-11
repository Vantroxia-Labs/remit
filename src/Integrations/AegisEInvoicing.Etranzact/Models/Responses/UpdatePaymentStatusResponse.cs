using AegisEInvoicing.Etranzact.Models;

namespace AegisEInvoicing.Etranzact.Models.Responses;

/// <summary>
/// Response from the update payment status endpoint.
/// PATCH /api/v1/app/invoice/update/{irn}
/// </summary>
public sealed class UpdatePaymentStatusResponse : EtranzactResponse<object?>
{
}
