using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Application.Features.BusinessManagement.DTOs;

public record BusinessDto(
    Guid Id,
    string Name,
    string Description,
    string BusinessRegistrationNumber,
    string TIN,
    string ServiceId,
    AddressDto RegisteredAddress,
    string ContactEmail,
    string ContactPhone,
    BusinessStatus Status,
    string? StatusReason,
    Guid AdminUserId,
    string AdminUserName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public record BusinessSummaryDto(
    Guid Id,
    string Name,
    string TIN,
    BusinessStatus Status,
    string? AdminUserName,
    DateTimeOffset CreatedAt);

public record BusinessUsersSummaryDto(
    Guid Id,
    string Name,
    string TIN,
    string InvoicePrefix,
    AddressDto RegisteredAddress,
    BusinessStatus Status,
    List<User>? AdminUser,
    DateTimeOffset CreatedAt);

public record AddressDto(
    string Street,
    string City,
    string? State,
    string Country,
    string? PostalCode);

public record BusinessStatisticsDto(
    int TotalBusinesses,
    int PendingBusinesses,
    int ActiveBusinesses,
    int InactiveBusinesses);

public record BusinessSubscriptionDto(
    string PlanName,
    double MonthlyPrice,
    SubscriptionStatus Status,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    DateTimeOffset? NextBillingDate);