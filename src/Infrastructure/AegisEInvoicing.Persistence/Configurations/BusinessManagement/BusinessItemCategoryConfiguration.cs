using AegisEInvoicing.Domain.Entities.BusinessManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.BusinessManagement;

/// <summary>
/// Entity Framework configuration for BusinessItemCategory junction entity
/// Configures the many-to-many relationship between BusinessItem and ItemCategory
/// </summary>
public class BusinessItemCategoryConfiguration : IEntityTypeConfiguration<BusinessItemItemCategory>
{
    public void Configure(EntityTypeBuilder<BusinessItemItemCategory> builder)
    {
        builder.ToTable("BusinessItemItemCategory");

        // Composite primary key
        builder.HasKey(x => new { x.BusinessItemId, x.ItemCategoryId });

        // Foreign key to BusinessItem
        builder.HasOne(x => x.BusinessItem)
            .WithMany(bi => bi.ItemCategories)
            .HasForeignKey(x => x.BusinessItemId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to ItemCategory
        builder.HasOne(x => x.ItemCategory)
            .WithMany(ic => ic.BusinessItems)
            .HasForeignKey(x => x.ItemCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(x => x.BusinessItemId)
            .HasDatabaseName("IX_BusinessItemCategories_BusinessItemId");

        builder.HasIndex(x => x.ItemCategoryId)
            .HasDatabaseName("IX_BusinessItemCategories_ItemCategoryId");

        // Audit fields configuration
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

        // Soft delete filter
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
