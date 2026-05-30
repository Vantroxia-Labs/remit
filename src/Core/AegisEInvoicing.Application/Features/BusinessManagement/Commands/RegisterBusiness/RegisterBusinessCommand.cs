using AegisEInvoicing.Domain.Entities.BusinessManagement;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.RegisterBusiness;

/// <summary>
/// Initiates self-service business registration. Creates a pending registration record
/// and returns a Paystack payment URL. The business is fully activated after payment
/// is confirmed via the Paystack webhook.
/// </summary>
public record RegisterBusinessCommand(
    string AdminFirstName,
    string AdminLastName,
    string AdminEmail,
    string AdminPhone,
    string BusinessName,
    IReadOnlyList<Guid> PlatformSubscriptionIds,
    BillingCycle BillingCycle,
    string? Tin = null) : IRequest<RegisterBusinessResult>;

public record RegisterBusinessResult(
    bool IsSuccess,
    string Message,
    string? PaymentUrl = null,
    string? Reference = null,
    Guid? PendingRegistrationId = null);
