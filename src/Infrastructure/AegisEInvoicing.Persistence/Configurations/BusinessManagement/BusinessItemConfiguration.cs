using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for BusinessItem entity
/// Configures table structure, relationships, indexes, and constraints for business items
/// </summary>
public class BusinessItemConfiguration : IEntityTypeConfiguration<BusinessItem>
{
    public void Configure(EntityTypeBuilder<BusinessItem> builder)
    {
        // Table configuration
        builder.ToTable("BusinessItems");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.ItemId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ItemCategoryId)
            .IsRequired();

        builder.Property(x => x.ItemDescription)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(x => x.UnitPrice)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.BusinessID)
            .IsRequired();

        // Value object configurations

        // ItemType as string column for readability
        builder.Property(x => x.ItemType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // ServiceCode as owned entity
        builder.OwnsOne(x => x.ServiceCode, sc =>
        {
            sc.Property(p => p.Code)
                .HasColumnName("ServiceCode")
                .IsRequired()
                .HasMaxLength(50);

            sc.Property(p => p.Name)
                .HasColumnName("ServiceCodeName")
                .IsRequired()
                .HasMaxLength(200);
        });

        // Navigation properties
        builder.HasOne(x => x.Business)
            .WithMany(b => b.BusinessItems)
            .HasForeignKey(x => x.BusinessID)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ItemCategory)
            .WithMany()
            .HasForeignKey(x => x.ItemCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many relationship with ItemCategory through junction entity
        builder.HasMany(x => x.ItemCategories)
            .WithOne(ic => ic.BusinessItem)
            .HasForeignKey(ic => ic.BusinessItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed navigation properties to prevent auto-generated junction table
        builder.Ignore(x => x.Categories);

        // One-to-many relationship with InvoiceItems
        builder.HasMany(x => x.InvoiceItems)
            .WithOne(ii => ii.BusinessItem)
            .HasForeignKey(ii => ii.BusinessItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.ItemId)
            .IsUnique()
            .HasDatabaseName("IX_BusinessItems_ItemId");

        builder.HasIndex(x => x.BusinessID)
            .HasDatabaseName("IX_BusinessItems_BusinessId");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_BusinessItems_Name");

        builder.HasIndex(x => x.ItemCategoryId)
            .HasDatabaseName("IX_BusinessItems_ItemCategoryId");

        // Composite indexes for common queries
        builder.HasIndex(x => new { x.BusinessID, x.ItemCategoryId })
            .HasDatabaseName("IX_BusinessItems_BusinessId_ItemCategoryId");

        builder.HasIndex(x => new { x.BusinessID, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_BusinessItems_BusinessId_Name")
            .HasFilter("\"IsDeleted\" = false");

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