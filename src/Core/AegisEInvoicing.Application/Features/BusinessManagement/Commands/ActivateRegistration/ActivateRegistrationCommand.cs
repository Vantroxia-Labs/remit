using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.ActivateRegistration;

/// <summary>
/// Activates a pending business registration after successful Paystack payment.
/// Creates the Business, Admin User, Subscription, FlowRules, SFTP user (if SFTP plan),
/// API key (if API plan), and sends the appropriate welcome email.
/// </summary>
public record ActivateRegistrationCommand(
    string PaystackReference,
    DateTimeOffset PaidAt) : IRequest<ActivateRegistrationResult>;

public record ActivateRegistrationResult(
    bool IsSuccess,
    string Message,
    Guid? BusinessId = null,
    bool AlreadyActivated = false);
