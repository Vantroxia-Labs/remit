using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SubscriptionKey entity
/// Configures table structure, indexes, and constraints for subscription keys
/// </summary>
public class SubscriptionKeyConfiguration : IEntityTypeConfiguration<SubscriptionKey>
{
    public void Configure(EntityTypeBuilder<SubscriptionKey> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("SubscriptionKeys");

        builder.HasKey(sk => sk.Id);

        builder.Property(sk => sk.Id)
            .IsRequired();

        builder.Property(sk => sk.Key)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(sk => sk.BusinessName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sk => sk.ContactEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(sk => sk.ContactPhone)
            .HasMaxLength(50);

        builder.Property(sk => sk.ExpiryDate)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(sk => sk.IsActive)
            .IsRequired();

        builder.Property(sk => sk.IsUsed)
            .IsRequired();

        builder.Property(sk => sk.UsedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(sk => sk.UsedBy);

        builder.Property(sk => sk.UsageNotes)
            .HasMaxLength(1000);

        builder.Property(sk => sk.MaxUsers)
            .IsRequired();

        builder.Property(sk => sk.MaxBusinesses)
            .IsRequired();

        builder.Property(sk => sk.Features)
            .HasMaxLength(1000);

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(sk => sk.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(sk => sk.CreatedBy)
            .IsRequired();

        builder.Property(sk => sk.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(sk => sk.UpdatedBy);

        builder.Property(sk => sk.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(sk => sk.DeletedBy);

        builder.Property(sk => sk.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(sk => sk.Key)
            .IsUnique()
            .HasDatabaseName("IX_SubscriptionKeys_Key");

        builder.HasIndex(sk => sk.IsActive)
            .HasDatabaseName("IX_SubscriptionKeys_IsActive");

        builder.HasIndex(sk => sk.IsUsed)
            .HasDatabaseName("IX_SubscriptionKeys_IsUsed");

        builder.HasIndex(sk => sk.ExpiryDate)
            .HasDatabaseName("IX_SubscriptionKeys_ExpiryDate");

        builder.HasIndex(sk => sk.ContactEmail)
            .HasDatabaseName("IX_SubscriptionKeys_ContactEmail");

        builder.HasIndex(sk => sk.IsDeleted)
            .HasDatabaseName("IX_SubscriptionKeys_IsDeleted");

        // Composite indexes for common queries
        builder.HasIndex(sk => new { sk.IsActive, sk.ExpiryDate })
            .HasDatabaseName("IX_SubscriptionKeys_IsActive_ExpiryDate");
        builder.HasIndex(sk => new { sk.IsUsed, sk.UsedAt })
            .HasDatabaseName("IX_SubscriptionKeys_IsUsed_UsedAt");
        builder.HasIndex(sk => new { sk.IsDeleted, sk.IsActive })
            .HasDatabaseName("IX_SubscriptionKeys_IsDeleted_IsActive");

        // Add soft delete query filter
        builder.HasQueryFilter(sk => !sk.IsDeleted);

        // Ignore domain events
        builder.Ignore(sk => sk.DomainEvents);
    }
}