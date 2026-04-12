using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Handlers;

public class GetDashboardStatsQueryHandler(
    IApplicationDbContext context,
    ILogger<GetDashboardStatsQueryHandler> logger)
    : IRequestHandler<GetDashboardStatsQuery, KMPGDashboardStatsDto>
{
    public async Task<KMPGDashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;
            var startOfMonth = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

            // ── Business & Subscription queries ─────────────────────────────
            var businesses = await context.Businesses
                .AsNoTracking()
                .Include(b => b.Subscription)
                    .ThenInclude(s => s != null ? s.PlatformSubscription : null)
                .Where(b => !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var expiredSubscriptions = businesses.Count(b =>
                b.Subscription != null && b.Subscription.EndDate < now);

            var portalBusinesses = businesses.Count(b =>
                b.Subscription?.PlatformSubscription?.Tier == SubscriptionTier.SaaS);
            var sftpBusinesses = businesses.Count(b =>
                b.Subscription?.PlatformSubscription?.Tier == SubscriptionTier.SFTP);
            var apiBusinesses = businesses.Count(b =>
                b.Subscription?.PlatformSubscription?.Tier == SubscriptionTier.ApiOnly);
            var onPremBusinesses = businesses.Count(b =>
                b.DeploymentMode == DeploymentMode.OnPremise);

            var pendingOnboardings = await context.BusinessOnboardings
                .AsNoTracking()
                .Where(o => !o.IsDeleted && !o.IsCompleted)
                .CountAsync(cancellationToken);

            // ── Invoice queries ──────────────────────────────────────────────
            var invoiceStats = await context.Invoices
                .AsNoTracking()
                .Where(i => !i.IsDeleted)
                .GroupBy(i => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    ThisMonth = g.Count(i => i.CreatedAt >= startOfMonth),
                    Draft = g.Count(i => i.Status == InvoiceStatus.DRAFT),
                    PendingApproval = g.Count(i => i.Status == InvoiceStatus.PENDING_APPROVAL),
                    Submitted = g.Count(i => i.Status == InvoiceStatus.SUBMITTED || i.Status == InvoiceStatus.TRANSMITTED || i.Status == InvoiceStatus.COMPLETELYTRANSMITTED),
                    Confirmed = g.Count(i => i.Status == InvoiceStatus.COMPLETELYTRANSMITTED || i.Status == InvoiceStatus.ACKNOWLEDGED),
                    Rejected = g.Count(i => i.Status == InvoiceStatus.REJECTED || i.Status == InvoiceStatus.FAILED),
                    PortalSource = g.Count(i => i.Source == InvoiceSource.PORTAL),
                    SftpSource = g.Count(i => i.Source == InvoiceSource.SFTP),
                    ErpSource = g.Count(i => i.Source == InvoiceSource.ERP),
                    TotalValue = g.Sum(i => (decimal?)i.TotalAmount) ?? 0m,
                    TotalVat = g.Sum(i => (decimal?)i.TotalTaxAmount) ?? 0m,
                    ValueThisMonth = g.Where(i => i.CreatedAt >= startOfMonth).Sum(i => (decimal?)i.TotalAmount) ?? 0m,
                    VatThisMonth = g.Where(i => i.CreatedAt >= startOfMonth).Sum(i => (decimal?)i.TotalTaxAmount) ?? 0m,
                    Paid = g.Count(i => i.PaymentStatus == Domain.Enums.PaymentStatus.Paid),
                    Unpaid = g.Count(i => i.PaymentStatus == Domain.Enums.PaymentStatus.Pending),
                    Partial = g.Count(i => i.PaymentStatus == Domain.Enums.PaymentStatus.Partial),
                    HasIRN = g.Count(i => i.IRN != null && i.IRN != string.Empty),
                    PendingIRN = g.Count(i => (i.IRN == null || i.IRN == string.Empty) && i.Status != InvoiceStatus.DRAFT)
                })
                .FirstOrDefaultAsync(cancellationToken);

            var totalReceivedInvoices = await context.ReceivedInvoices
                .AsNoTracking()
                .Where(r => !r.IsDeleted)
                .CountAsync(cancellationToken);

            var pendingRegistrations = await context.PendingBusinessRegistrations
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Status == PendingRegistrationStatus.AwaitingPayment)
                .CountAsync(cancellationToken);

            return new KMPGDashboardStatsDto
            {
                // Business & subscription
                TotalBusinesses = businesses.Count,
                ActiveBusinesses = businesses.Count(b => b.IsActive),
                SuspendedBusinesses = businesses.Count(b => !b.IsActive),
                PendingOnboardings = pendingOnboardings,
                ExpiredSubscriptions = expiredSubscriptions,
                SaaSBusinesses = businesses.Count(b => b.DeploymentMode != DeploymentMode.OnPremise),
                OnPremiseBusinesses = onPremBusinesses,

                // Tier breakdown
                PortalPlanBusinesses = portalBusinesses,
                SftpPlanBusinesses = sftpBusinesses,
                ApiPlanBusinesses = apiBusinesses,

                // Invoices
                TotalInvoices = invoiceStats?.Total ?? 0,
                TotalInvoicesThisMonth = invoiceStats?.ThisMonth ?? 0,
                DraftInvoices = invoiceStats?.Draft ?? 0,
                PendingApprovalInvoices = invoiceStats?.PendingApproval ?? 0,
                SubmittedToNRS = invoiceStats?.Submitted ?? 0,
                ConfirmedByNRS = invoiceStats?.Confirmed ?? 0,
                RejectedInvoices = invoiceStats?.Rejected ?? 0,
                PortalCreatedInvoices = invoiceStats?.PortalSource ?? 0,
                SftpCreatedInvoices = invoiceStats?.SftpSource ?? 0,
                ApiCreatedInvoices = invoiceStats?.ErpSource ?? 0,

                // Financial
                TotalInvoiceValue = invoiceStats?.TotalValue ?? 0m,
                TotalVatCollected = invoiceStats?.TotalVat ?? 0m,
                TotalInvoiceValueThisMonth = invoiceStats?.ValueThisMonth ?? 0m,
                TotalVatThisMonth = invoiceStats?.VatThisMonth ?? 0m,

                // IRN & Compliance
                TotalIRNsGenerated = invoiceStats?.HasIRN ?? 0,
                PendingIRNs = invoiceStats?.PendingIRN ?? 0,

                // Payment
                PaidInvoices = invoiceStats?.Paid ?? 0,
                UnpaidInvoices = invoiceStats?.Unpaid ?? 0,
                PartiallyPaidInvoices = invoiceStats?.Partial ?? 0,

                // Received invoices
                TotalReceivedInvoices = totalReceivedInvoices,

                // Pending registrations
                PendingRegistrations = pendingRegistrations,

                // Platform revenue (future: sum subscription payments)
                PlatformRevenueTotal = 0m,
                PlatformRevenueThisMonth = 0m
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving dashboard statistics");
            throw;
        }
    }
}
