using AegisEInvoicing.Domain.Entities.BusinessManagement;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.AdminCreateBusiness;

/// <summary>
/// Creates a business directly on behalf of a client (Aegis Admin only).
/// No Paystack payment redirect — payment details are recorded as-is for audit purposes.
/// </summary>
public record AdminCreateBusinessCommand(
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPhone,
    string BusinessName,
    string BusinessDescription,
    IReadOnlyList<Guid> PlatformSubscriptionIds,
    BillingCycle BillingCycle,
    string PaymentReference,
    decimal PaymentAmountNaira,
    string? Tin = null) : IRequest<AdminCreateBusinessResult>;

public record AdminCreateBusinessResult(
    bool IsSuccess,
    string Message,
    Guid? BusinessId = null);
