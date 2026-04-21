using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Features.BusinessManagement.Queries;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
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
                .Include(b => b.Subscriptions)
                    .ThenInclude(s => s.PlatformSubscription)
                .Where(b => !b.IsDeleted)
                .ToListAsync(cancellationToken);

            var expiredSubscriptions = businesses.Count(b =>
                b.Subscriptions.Any(s => s.EndDate < now));

            var portalBusinesses = businesses.Count(b =>
                b.Subscriptions.Any(s => s.PlatformSubscription?.Tier == SubscriptionTier.SaaS));
            var sftpBusinesses = businesses.Count(b =>
                b.Subscriptions.Any(s => s.PlatformSubscription?.Tier == SubscriptionTier.SFTP));
            var apiBusinesses = businesses.Count(b =>
                b.Subscriptions.Any(s => s.PlatformSubscription?.Tier == SubscriptionTier.ApiOnly));
            var onPremBusinesses = businesses.Count(b =>
                b.DeploymentMode == DeploymentMode.OnPremise);

            var pendingOnboardings = await context.BusinessOnboardings
                .AsNoTracking()
                .Where(o => !o.IsDeleted && o.Status != BusinessOnboardingStatus.Completed && o.Status != BusinessOnboardingStatus.Cancelled)
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
                    Draft = g.Count(i => i.InvoiceStatus == InvoiceStatus.DRAFT),
                    PendingApproval = g.Count(i => i.InvoiceStatus == InvoiceStatus.PENDING_APPROVAL),
                    Submitted = g.Count(i => i.InvoiceStatus == InvoiceStatus.SUBMITTED || i.InvoiceStatus == InvoiceStatus.TRANSMITTED),
                    Confirmed = g.Count(i => i.InvoiceStatus == InvoiceStatus.TRANSMITTED || i.InvoiceStatus == InvoiceStatus.ACKNOWLEDGED),
                    Rejected = g.Count(i => i.InvoiceStatus == InvoiceStatus.REJECTED || i.InvoiceStatus == InvoiceStatus.FAILED),
                    PortalSource = g.Count(i => i.InvoiceSource == InvoiceSource.PORTAL),
                    SftpSource = g.Count(i => i.InvoiceSource == InvoiceSource.SFTP),
                    ErpSource = g.Count(i => i.InvoiceSource == InvoiceSource.ERP),
                    Paid = g.Count(i => i.PaymentStatus == PaymentStatus.Paid),
                    Unpaid = g.Count(i => i.PaymentStatus == PaymentStatus.Pending),
                    HasIRN = g.Count(i => i.FIRSSubmissionId != null),
                    PendingIRN = g.Count(i => i.FIRSSubmissionId == null && i.InvoiceStatus != InvoiceStatus.DRAFT)
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
                ActiveBusinesses = businesses.Count(b => b.Status == BusinessStatus.Active),
                SuspendedBusinesses = businesses.Count(b => b.Status == BusinessStatus.Suspended),
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
                TotalInvoiceValue = 0m,
                TotalVatCollected = 0m,
                TotalInvoiceValueThisMonth = 0m,
                TotalVatThisMonth = 0m,

                // IRN & Compliance
                TotalIRNsGenerated = invoiceStats?.HasIRN ?? 0,
                PendingIRNs = invoiceStats?.PendingIRN ?? 0,

                // Payment
                PaidInvoices = invoiceStats?.Paid ?? 0,
                UnpaidInvoices = invoiceStats?.Unpaid ?? 0,
                PartiallyPaidInvoices = 0,

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
