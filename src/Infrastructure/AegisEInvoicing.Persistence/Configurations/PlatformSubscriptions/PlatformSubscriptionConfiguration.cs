using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AegisEInvoicing.Domain.Entities;

namespace AegisEInvoicing.Persistence.Configurations.PlatformSubscriptions;

/// <summary>
/// Entity Framework configuration for PlatformSubscription entity
/// Configures table structure, relationships, indexes, and constraints for platform subscriptions
/// </summary>
public class PlatformSubscriptionConfiguration : IEntityTypeConfiguration<PlatformSubscription>
{
    public void Configure(EntityTypeBuilder<PlatformSubscription> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("PlatformSubscriptions");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Id)
            .IsRequired();

        builder.Property(pr => pr.PlanName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.Tier)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.MonthlyPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(pr => pr.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("NGN");

        // Ignore the computed Description property
        builder.Ignore(pr => pr.Description);

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(pr => pr.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(pr => pr.CreatedBy)
            .IsRequired();

        builder.Property(pr => pr.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(pr => pr.UpdatedBy);

        builder.Property(pr => pr.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(pr => pr.DeletedBy);

        builder.Property(pr => pr.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure relationships
        builder.HasMany(e => e.Subscriptions)
            .WithOne(u => u.PlatformSubscription)
            .HasForeignKey(u => u.PlatformSubscriptionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add indexes for performance with proper naming
        builder.HasIndex(pr => pr.PlanName)
            .IsUnique()
            .HasDatabaseName("IX_PlatformSubscriptions_PlanName");
        builder.HasIndex(pr => pr.Tier)
            .HasDatabaseName("IX_PlatformSubscriptions_Tier");
        builder.HasIndex(pr => pr.CreatedAt)
            .HasDatabaseName("IX_PlatformSubscriptions_CreatedAt");
        builder.HasIndex(pr => pr.IsDeleted)
            .HasDatabaseName("IX_PlatformSubscriptions_IsDeleted");

        // Composite indexes for common queries
        builder.HasIndex(pr => new { pr.Tier, pr.IsDeleted })
            .HasDatabaseName("IX_PlatformSubscriptions_Tier_IsDeleted");

        // Add soft delete query filter
        builder.HasQueryFilter(pr => !pr.IsDeleted);

        // Ignore domain events
        builder.Ignore(pr => pr.DomainEvents);
    }
}
