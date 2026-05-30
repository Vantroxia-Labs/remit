using AegisEInvoicing.Domain.Enums;
using MediatR;

namespace AegisEInvoicing.Application.Features.BusinessManagement.Queries;

public record GetDashboardStatsQuery : IRequest<KMPGDashboardStatsDto>
{
    public AppEnvironmentMode? EnvironmentMode { get; init; }
}

public record KMPGDashboardStatsDto
{
    // ── Business & Subscription Metrics ────────────────────────────────────
    public int TotalBusinesses { get; init; }
    public int ActiveBusinesses { get; init; }
    public int SuspendedBusinesses { get; init; }
    public int PendingOnboardings { get; init; }
    public int ExpiredSubscriptions { get; init; }
    public int SaaSBusinesses { get; init; }
    public int OnPremiseBusinesses { get; init; }

    // ── NRS-MBS Subscription Tier Breakdown ────────────────────────────────
    public int PortalPlanBusinesses { get; init; }    // SaaS tier
    public int SftpPlanBusinesses { get; init; }      // SFTP tier
    public int ApiPlanBusinesses { get; init; }       // ApiOnly tier

    // ── Invoice Metrics ────────────────────────────────────────────────────
    public long TotalInvoices { get; init; }
    public long TotalInvoicesThisMonth { get; init; }
    public long DraftInvoices { get; init; }
    public long PendingApprovalInvoices { get; init; }
    public long SubmittedToNRS { get; init; }          // SUBMITTED + CONFIRMED
    public long ConfirmedByNRS { get; init; }          // CONFIRMED only
    public long RejectedInvoices { get; init; }
    public long PortalCreatedInvoices { get; init; }  // Source = Portal
    public long SftpCreatedInvoices { get; init; }    // Source = SFTP
    public long ApiCreatedInvoices { get; init; }     // Source = API

    // ── Financial Metrics ──────────────────────────────────────────────────
    public decimal TotalInvoiceValue { get; init; }
    public decimal TotalVatCollected { get; init; }
    public decimal TotalInvoiceValueThisMonth { get; init; }
    public decimal TotalVatThisMonth { get; init; }

    // ── IRN & Compliance ───────────────────────────────────────────────────
    public long TotalIRNsGenerated { get; init; }
    public long PendingIRNs { get; init; }

    // ── Payment Status ─────────────────────────────────────────────────────
    public long PaidInvoices { get; init; }
    public long UnpaidInvoices { get; init; }
    public long PartiallyPaidInvoices { get; init; }

    // ── Received Invoices ──────────────────────────────────────────────────
    public long TotalReceivedInvoices { get; init; }

    // ── Pending Registrations ─────────────────────────────────────────────
    public int PendingRegistrations { get; init; }    // Awaiting Paystack payment

    // ── Platform Revenue (Aegis Admin only) ──────────────────────────────────
    public decimal PlatformRevenueTotal { get; init; }
    public decimal PlatformRevenueThisMonth { get; init; }
}