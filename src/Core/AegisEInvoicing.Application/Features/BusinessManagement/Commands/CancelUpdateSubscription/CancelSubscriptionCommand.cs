using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.CancelSubscription;

public record CancelSubscriptionCommand(string? reason = null) : IRequest<CancelSubscriptionResult>;


public record CancelSubscriptionResult(bool isSuccess,string message);