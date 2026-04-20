using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Entities.VendorManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace AegisEInvoicing.Application.Common.Interfaces;

/// <summary>
/// Application database context interface
/// </summary>
public interface IApplicationDbContext
{
    DbSet<OutboxEvent> OutboxEvents { get; }
    DbSet<IntegrationLog> IntegrationLogs { get; }
    DbSet<Business> Businesses { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceDraft> InvoiceDrafts { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Branch> Branches { get; }
    DbSet<FlowRule> FlowRules { get; }
    DbSet<FIRSApiConfiguration> FIRSApiConfigurations { get; }
    DbSet<AppProviderConfiguration> AppProviderConfigurations { get; }
    DbSet<BusinessFIRSApiConfiguration> BusinessFIRSApiConfigurations { get; }
    DbSet<BusinessOnboarding> BusinessOnboardings { get; }
    DbSet<SystemConfiguration> SystemConfigurations { get; }
    DbSet<SubscriptionKey> SubscriptionKeys { get; }
    DbSet<ApiUsageTracking> ApiUsageTrackings { get; }
    DbSet<ApiUsageSummary> ApiUsageSummaries { get; }
    DbSet<InvoiceTransmissionQueue> InvoiceTransmissionQueues { get; }
    DbSet<InvoiceApprovalHistory> InvoiceApprovalHistories { get; }
    DbSet<Party> Parties { get; }
    DbSet<ItemCategory> ItemCategories { get; }
    DbSet<BusinessItem> BusinessItems { get; }
    DbSet<BusinessItemPriceHistory> BusinessItemPriceHistories { get; }
    DbSet<BusinessItemItemCategory> BusinessItemItemCategory { get; }
    DbSet<ReceivedInvoice> ReceivedInvoices { get; }

    // User Management entities
    DbSet<User> Users { get; }
    DbSet<PlatformRole> PlatformRoles { get; }
    DbSet<UserRoleAssignment> UserRoleAssignments { get; }
    DbSet<UserSession> UserSessions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Subscription> Subscriptions { get; }
    DbSet<PlatformSubscription> PlatformSubscriptions { get; }
    DbSet<SFTPUser> SFTPUsers { get; }
    DbSet<PendingBusinessRegistration> PendingBusinessRegistrations { get; }
    DbSet<VatSchedule> VatSchedules { get; }
    DbSet<VatScheduleItem> VatScheduleItems { get; }
    DbSet<InputVatScheduleItem> InputVatScheduleItems { get; }
    DbSet<WhtSchedule> WhtSchedules { get; }
    DbSet<WhtScheduleItem> WhtScheduleItems { get; }

    // Vendor Management
    DbSet<VendorGroup> VendorGroups { get; }
    DbSet<Vendor> Vendors { get; }
    DbSet<InvoiceBroadcast> InvoiceBroadcasts { get; }
    DbSet<InvoiceBroadcastVendor> InvoiceBroadcastVendors { get; }

    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}