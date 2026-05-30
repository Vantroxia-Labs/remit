using AegisEInvoicing.Domain.Entities.VendorManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.ToTable("Vendors");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.BusinessName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(v => v.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(v => v.Phone)
            .HasMaxLength(50);

        builder.Property(v => v.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(v => v.BusinessId)
            .IsRequired();

        builder.Property(v => v.VendorGroupId)
            .IsRequired();

        builder.HasOne(v => v.Business)
            .WithMany()
            .HasForeignKey(v => v.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(v => v.VendorGroup)
            .WithMany(g => g.Vendors)
            .HasForeignKey(v => v.VendorGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(v => v.BroadcastVendors)
            .WithOne(bv => bv.Vendor)
            .HasForeignKey(bv => bv.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => v.BusinessId)
            .HasDatabaseName("IX_Vendors_BusinessId");

        builder.HasIndex(v => v.VendorGroupId)
            .HasDatabaseName("IX_Vendors_VendorGroupId");

        builder.HasIndex(v => new { v.BusinessId, v.Email })
            .IsUnique()
            .HasDatabaseName("IX_Vendors_BusinessId_Email");

        builder.HasIndex(v => v.IsDeleted)
            .HasDatabaseName("IX_Vendors_IsDeleted");

        builder.HasQueryFilter(v => !v.IsDeleted);

        builder.Ignore(v => v.DomainEvents);
    }
}
