using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.BusinessManagement;

/// <summary>
/// Entity Framework configuration for ItemCategory entity
/// Configures table structure, relationships, indexes, and constraints for item categories
/// </summary>
public class ItemCategoryConfiguration : IEntityTypeConfiguration<ItemCategory>
{
    public void Configure(EntityTypeBuilder<ItemCategory> builder)
    {
        // Table configuration
        builder.ToTable("ItemCategories");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.BusinessID)
            .IsRequired();

        // Navigation properties
        builder.HasOne(x => x.Business)
            .WithMany(b => b.ItemCategories)
            .HasForeignKey(x => x.BusinessID)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many relationship with BusinessItem through junction entity
        builder.HasMany(x => x.BusinessItems)
            .WithOne(bic => bic.ItemCategory)
            .HasForeignKey(bic => bic.ItemCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed navigation property to prevent auto-generated junction table
        builder.Ignore(x => x.Items);

        // Indexes
        builder.HasIndex(x => x.BusinessID)
            .HasDatabaseName("IX_ItemCategories_BusinessId");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_ItemCategories_Name");

        // Composite indexes for common queries
        builder.HasIndex(x => new { x.BusinessID, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_ItemCategories_BusinessId_Name");

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedBy);

        builder.Property(x => x.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DeletedBy);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Add soft delete query filter
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}