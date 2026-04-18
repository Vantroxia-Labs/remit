using AegisEInvoicing.Domain.Entities.VendorManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

public class VendorGroupConfiguration : IEntityTypeConfiguration<VendorGroup>
{
    public void Configure(EntityTypeBuilder<VendorGroup> builder)
    {
        builder.ToTable("VendorGroups");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(v => v.Description)
            .HasMaxLength(500);

        builder.Property(v => v.BusinessId)
            .IsRequired();

        builder.HasOne(v => v.Business)
            .WithMany()
            .HasForeignKey(v => v.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.Vendors)
            .WithOne(v => v.VendorGroup)
            .HasForeignKey(v => v.VendorGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.BusinessId)
            .HasDatabaseName("IX_VendorGroups_BusinessId");

        builder.HasIndex(v => new { v.BusinessId, v.Name })
            .IsUnique()
            .HasDatabaseName("IX_VendorGroups_BusinessId_Name");

        builder.HasIndex(v => v.IsDeleted)
            .HasDatabaseName("IX_VendorGroups_IsDeleted");

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.Ignore(v => v.DomainEvents);
    }
}
