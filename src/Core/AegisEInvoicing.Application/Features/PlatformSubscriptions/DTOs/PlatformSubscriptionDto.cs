using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Application.Features.PlatformSubscriptions.DTOs;

public sealed record PlatformSubscriptionDto(
    Guid Id,
    string PlanName,
    SubscriptionTier Tier,
    double MonthlyPrice,
    string Currency,
    string Description,
    int NumberOfSubscriptions);