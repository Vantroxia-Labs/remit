using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Entities.VendorManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AegisEInvoicing.Persistence;

/// <summary>
/// Application database context
/// </summary>
public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    ICurrentUserService currentUserService,
    IDateTime dateTime) : DbContext(options), IApplicationDbContext
{
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IDateTime _dateTime = dateTime;

    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();
    public DbSet<IntegrationLog> IntegrationLogs => Set<IntegrationLog>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<InvoiceApprovalHistory> InvoiceApprovalHistories => Set<InvoiceApprovalHistory>();
    public DbSet<InvoiceBillingReference> BillingReferences => Set<InvoiceBillingReference>();
    public DbSet<InvoiceDispatchDocumentReference> DispatchDocumentReferences => Set<InvoiceDispatchDocumentReference>();
    public DbSet<InvoiceReceiptDocumentReference> ReceiptDocumentReferences => Set<InvoiceReceiptDocumentReference>();
    public DbSet<InvoiceOriginatorDocumentReference> OriginatorDocumentReferences => Set<InvoiceOriginatorDocumentReference>();
    public DbSet<InvoiceContractDocumentReference> ContractDocumentReferences => Set<InvoiceContractDocumentReference>();
    public DbSet<InvoiceAdditionalDocumentReference> AdditionalDocumentReferences => Set<InvoiceAdditionalDocumentReference>();
    public DbSet<Party> Parties => Set<Party>();
    public DbSet<BusinessItem> BusinessItems => Set<BusinessItem>();
    public DbSet<BusinessItemPriceHistory> BusinessItemPriceHistories => Set<BusinessItemPriceHistory>();
    public DbSet<BusinessItemItemCategory> BusinessItemItemCategory => Set<BusinessItemItemCategory>();
    public DbSet<ItemCategory> ItemCategories => Set<ItemCategory>();
    public DbSet<ReceivedInvoice> ReceivedInvoices => Set<ReceivedInvoice>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<FlowRule> FlowRules => Set<FlowRule>();
    public DbSet<FIRSApiConfiguration> FIRSApiConfigurations => Set<FIRSApiConfiguration>();
    public DbSet<AppProviderConfiguration> AppProviderConfigurations => Set<AppProviderConfiguration>();
    public DbSet<BusinessFIRSApiConfiguration> BusinessFIRSApiConfigurations => Set<BusinessFIRSApiConfiguration>();
    public DbSet<BusinessOnboarding> BusinessOnboardings => Set<BusinessOnboarding>();
    public DbSet<SystemConfiguration> SystemConfigurations => Set<SystemConfiguration>();
    public DbSet<SubscriptionKey> SubscriptionKeys => Set<SubscriptionKey>();
    public DbSet<ApiUsageTracking> ApiUsageTrackings => Set<ApiUsageTracking>();
    public DbSet<ApiUsageSummary> ApiUsageSummaries => Set<ApiUsageSummary>();
    public DbSet<InvoiceTransmissionQueue> InvoiceTransmissionQueues => Set<InvoiceTransmissionQueue>();


    // User Management entities
    public DbSet<User> Users => Set<User>();
    public DbSet<PlatformRole> PlatformRoles => Set<PlatformRole>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<PlatformSubscription> PlatformSubscriptions => Set<PlatformSubscription>();
    public DbSet<SFTPUser> SFTPUsers => Set<SFTPUser>();
    public DbSet<PendingBusinessRegistration> PendingBusinessRegistrations => Set<PendingBusinessRegistration>();
    public DbSet<VatSchedule> VatSchedules => Set<VatSchedule>();
    public DbSet<VatScheduleItem> VatScheduleItems => Set<VatScheduleItem>();
    public DbSet<InputVatScheduleItem> InputVatScheduleItems => Set<InputVatScheduleItem>();
    public DbSet<WhtSchedule> WhtSchedules => Set<WhtSchedule>();
    public DbSet<WhtScheduleItem> WhtScheduleItems => Set<WhtScheduleItem>();

    // Vendor Management
    public DbSet<VendorGroup> VendorGroups => Set<VendorGroup>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<InvoiceBroadcast> InvoiceBroadcasts => Set<InvoiceBroadcast>();
    public DbSet<InvoiceBroadcastVendor> InvoiceBroadcastVendors => Set<InvoiceBroadcastVendor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Apply snake_case naming convention for PostgreSQL
        // Note: This should be configured in the options, not here
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await DispatchDomainEventsAsync();

        var currentUserId = _currentUserService.UserId;

        if (currentUserId is null || currentUserId == Guid.Empty)
            currentUserId = Guid.Parse("9c17ea5c-483c-44f8-97e8-c364e6739949");

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedBy == Guid.Empty)
                    {
                        entry.Entity.CreatedBy = currentUserId.Value;
                    }
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = currentUserId;
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    break;

                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Entity.DeletedBy = currentUserId;
                    entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
                    entry.Entity.IsDeleted = true;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return await Database.BeginTransactionAsync(cancellationToken);
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker.Entries<IEntity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.ClearDomainEvents());

        // Store events in outbox for processing
        foreach (var domainEvent in domainEvents)
        {
            var outboxEvent = new OutboxEvent
            {
                Id = Guid.CreateVersion7(),
                EventType = domainEvent.GetType().AssemblyQualifiedName!,
                EventData = JsonSerializer.Serialize(domainEvent),
                OccurredOnUtc = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                Status = OutboxEventStatus.Pending
            };

            OutboxEvents.Add(outboxEvent);
        }

        await Task.CompletedTask;
    }
}

public static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var sb = new StringBuilder();
        sb.Append(char.ToLower(str[0]));

        for (int i = 1; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]))
            {
                sb.Append('_');
                sb.Append(char.ToLower(str[i]));
            }
            else
            {
                sb.Append(str[i]);
            }
        }

        return sb.ToString();
    }
}