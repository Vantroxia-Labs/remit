using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.Portal.API.Controllers;

/// <summary>
/// Returns real-time in-app notifications derived from live business data.
/// Tenant-scoped: business users only see their own data; Aegis users see platform-wide events.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
public class NotificationsController(
    IApplicationDbContext context,
    ICurrentUserService currentUser) : BaseApiController
{
    private const int RecentDays = 7;
    private const int ExpiryWarningDays = 30;
    private const int MaxItems = 30;

    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
    {
        var results = new List<NotificationDto>();
        var now = DateTimeOffset.UtcNow;
        var cutoff = now.AddDays(-RecentDays);
        var expiryThreshold = now.AddDays(ExpiryWarningDays);

        if (currentUser.IsAegisUser)
        {
            // ── New businesses registered recently ──────────────────────────
            var newBusinesses = await context.Businesses
                .Where(b => b.CreatedAt >= cutoff && !b.IsDeleted)
                .OrderByDescending(b => b.CreatedAt)
                .Take(10)
                .Select(b => new { b.Id, b.Name, b.CreatedAt })
                .ToListAsync(cancellationToken);

            results.AddRange(newBusinesses.Select(b => new NotificationDto
            {
                Id = $"biz-{b.Id}",
                Type = "new_business",
                Title = "New business registered",
                Body = $"{b.Name} has completed registration on the platform.",
                Timestamp = b.CreatedAt,
                Read = false,
                AegisOnly = true,
            }));

            // ── Subscriptions expiring within 30 days ───────────────────────
            var expiringSubs = await context.Subscriptions
                .Include(s => s.Business)
                .Include(s => s.PlatformSubscription)
                .Where(s => !s.IsDeleted
                         && s.Status == SubscriptionStatus.Active
                         && s.EndDate >= now
                         && s.EndDate <= expiryThreshold)
                .OrderBy(s => s.EndDate)
                .Take(10)
                .ToListAsync(cancellationToken);

            results.AddRange(expiringSubs.Select(s => new NotificationDto
            {
                Id = $"exp-aegis-{s.Id}",
                Type = "plan_expiry",
                Title = "Business subscription expiring",
                Body = $"{s.Business.Name}'s {s.PlatformSubscription.PlanName} expires on {s.EndDate:dd MMM yyyy}. Consider reaching out to renew.",
                Timestamp = s.EndDate.AddDays(-7),
                Read = false,
                AegisOnly = true,
            }));
        }
        else if (currentUser.BusinessId.HasValue)
        {
            var businessId = currentUser.BusinessId.Value;

            // ── Recent invoice activity (last 7 days) ───────────────────────
            var recentInvoices = await context.Invoices
                .Where(i => i.BusinessId == businessId && !i.IsDeleted && i.CreatedAt >= cutoff)
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .Select(i => new { i.Id, i.InvoiceCode, i.InvoiceStatus, i.CreatedAt })
                .ToListAsync(cancellationToken);

            foreach (var inv in recentInvoices)
            {
                var (type, title, body) = inv.InvoiceStatus switch
                {
                    InvoiceStatus.CREATED or InvoiceStatus.PENDING_APPROVAL =>
                        ("invoice_raised", "New invoice raised",
                         $"Invoice {inv.InvoiceCode} has been raised and is awaiting processing."),
                    InvoiceStatus.APPROVED =>
                        ("invoice_approved", "Invoice approved",
                         $"Invoice {inv.InvoiceCode} has been approved."),
                    InvoiceStatus.TRANSMITTED or InvoiceStatus.COMPLETELYTRANSMITTED =>
                        ("invoice_transmitted", "Invoice transmitted to NRS",
                         $"Invoice {inv.InvoiceCode} was successfully transmitted to NRS."),
                    InvoiceStatus.REJECTED or InvoiceStatus.FAILED or InvoiceStatus.VALIDATIONFAILED =>
                        ("invoice_rejected", "Invoice rejected",
                         $"Invoice {inv.InvoiceCode} was rejected. Please review and resubmit."),
                    _ => (null!, null!, null!)
                };

                if (type is null) continue;

                results.Add(new NotificationDto
                {
                    Id = $"inv-{inv.Id}",
                    Type = type,
                    Title = title,
                    Body = body,
                    Timestamp = inv.CreatedAt,
                    Read = false,
                });
            }

            // ── Own subscriptions expiring within 30 days ───────────────────
            var myExpiringSubs = await context.Subscriptions
                .Include(s => s.PlatformSubscription)
                .Where(s => s.BusinessId == businessId
                         && !s.IsDeleted
                         && s.Status == SubscriptionStatus.Active
                         && s.EndDate >= now
                         && s.EndDate <= expiryThreshold)
                .ToListAsync(cancellationToken);

            results.AddRange(myExpiringSubs.Select(s => new NotificationDto
            {
                Id = $"exp-{s.Id}",
                Type = "plan_expiry",
                Title = "Subscription expiring soon",
                Body = $"Your {s.PlatformSubscription.PlanName} expires on {s.EndDate:dd MMM yyyy}. Renew to avoid service interruption.",
                Timestamp = s.EndDate.AddDays(-ExpiryWarningDays / 4),
                Read = false,
            }));
        }

        var sorted = results
            .OrderByDescending(n => n.Timestamp)
            .Take(MaxItems)
            .ToList();

        return Success(sorted);
    }
}

/// <summary>DTO returned by GET /notifications</summary>
public sealed record NotificationDto
{
    public string Id { get; init; } = string.Empty;
    /// <summary>Matches the NotifType union on the frontend.</summary>
    public string Type { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    /// <summary>ISO 8601 timestamp — frontend converts to relative string.</summary>
    public DateTimeOffset Timestamp { get; init; }
    public bool Read { get; init; }
    public bool AegisOnly { get; init; }
    public bool AdminOnly { get; init; }
}
