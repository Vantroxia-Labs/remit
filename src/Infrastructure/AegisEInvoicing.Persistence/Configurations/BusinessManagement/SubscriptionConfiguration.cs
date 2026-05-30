using AegisEInvoicing.Domain.Entities.BusinessManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.BusinessManagement;

/// <summary>
/// Entity Framework configuration for Subscription entity
/// Configures table structure, relationships, indexes, and constraints for subscriptions
/// </summary>
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("Subscriptions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .IsRequired();

        builder.Property(s => s.BusinessId)
            .IsRequired();

        builder.Property(s => s.PlatformSubscriptionId)
             .IsRequired();

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.StartDate)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.EndDate)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.LastBillingDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.NextBillingDate)
            .HasColumnType("timestamp with time zone");

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(s => s.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.CreatedBy)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.UpdatedBy);

        builder.Property(s => s.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.DeletedBy);

        builder.Property(s => s.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure relationship with Business — one-to-many
        builder.HasOne(s => s.Business)
            .WithMany(m => m.Subscriptions)
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.PlatformSubscription)
               .WithMany(m => m.Subscriptions)
               .HasForeignKey(s => s.PlatformSubscriptionId)
               .OnDelete(DeleteBehavior.Restrict);

        // Add indexes for performance with proper naming
        builder.HasIndex(s => s.BusinessId)
            .HasDatabaseName("IX_Subscriptions_BusinessId");
        builder.HasIndex(s => s.PlatformSubscriptionId)
            .HasDatabaseName("IX_Subscriptions_PlatformSubscriptionId");
        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_Subscriptions_Status");
        builder.HasIndex(s => s.StartDate)
            .HasDatabaseName("IX_Subscriptions_StartDate");
        builder.HasIndex(s => s.EndDate)
            .HasDatabaseName("IX_Subscriptions_EndDate");
        builder.HasIndex(s => s.CreatedAt)
            .HasDatabaseName("IX_Subscriptions_CreatedAt");
        builder.HasIndex(s => s.IsDeleted)
            .HasDatabaseName("IX_Subscriptions_IsDeleted");

        // Composite indexes for common queries
        builder.HasIndex(s => new { s.Status, s.EndDate })
            .HasDatabaseName("IX_Subscriptions_Status_EndDate");
        builder.HasIndex(s => new { s.IsDeleted, s.Status })
            .HasDatabaseName("IX_Subscriptions_IsDeleted_Status");

        // Add soft delete query filter
        builder.HasQueryFilter(s => !s.IsDeleted);

        // Ignore domain events
        builder.Ignore(s => s.DomainEvents);
    }
}