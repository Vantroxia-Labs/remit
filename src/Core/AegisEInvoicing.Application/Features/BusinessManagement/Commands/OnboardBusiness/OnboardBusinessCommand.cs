using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.ValueObjects;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Commands.OnboardBusiness;

public record OnboardBusinessCommand(
    string BusinessName,
    string TIN,
    string BusinessRegistrationNumber,
    Address RegisteredAddress,
    string InvoicePrefix,
    Guid FIRSBusinessId,
    string Industry,
    string ContactEmail,
    string ContactPhone,    
    string Description,
    string ServiceId,
    string AdminFirstName,
    string AdminLastName,
    Guid PlatformSubscriptionId,
    int Duration,
    DateOnly SubscriptionStartDate,
    DeploymentMode? DeploymentMode = null) : IRequest<OnboardBusinessResult>, ITransactionalCommand
{
    public DateTimeOffset SubscriptionStartDateTimeOffset =>
       new DateTimeOffset(SubscriptionStartDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    public DateTimeOffset SubscriptionEndDateTimeOffset =>
        new DateTimeOffset(SubscriptionStartDate.AddMonths(Duration).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}