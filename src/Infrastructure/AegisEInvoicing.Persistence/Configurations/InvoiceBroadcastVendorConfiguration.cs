using AegisEInvoicing.Domain.Entities.VendorManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

public class InvoiceBroadcastVendorConfiguration : IEntityTypeConfiguration<InvoiceBroadcastVendor>
{
    public void Configure(EntityTypeBuilder<InvoiceBroadcastVendor> builder)
    {
        builder.ToTable("InvoiceBroadcastVendors");

        builder.HasKey(bv => bv.Id);

        builder.Property(bv => bv.InvoiceBroadcastId)
            .IsRequired();

        builder.Property(bv => bv.VendorId)
            .IsRequired();

        builder.Property(bv => bv.InvoiceId);

        builder.Property(bv => bv.Token)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(bv => bv.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(bv => bv.VerificationCode)
            .HasMaxLength(10);

        builder.Property(bv => bv.VerificationCodeExpiresAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(bv => bv.EmailVerifiedAt)
            .HasColumnType("timestamp with time zone");

        builder.HasOne(bv => bv.InvoiceBroadcast)
            .WithMany(b => b.BroadcastVendors)
            .HasForeignKey(bv => bv.InvoiceBroadcastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bv => bv.Vendor)
            .WithMany(v => v.BroadcastVendors)
            .HasForeignKey(bv => bv.VendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bv => bv.Invoice)
            .WithMany()
            .HasForeignKey(bv => bv.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // One vendor can only appear once per broadcast
        builder.HasIndex(bv => new { bv.InvoiceBroadcastId, bv.VendorId })
            .IsUnique()
            .HasDatabaseName("IX_InvoiceBroadcastVendors_BroadcastId_VendorId");

        // Token must be globally unique for secure link resolution
        builder.HasIndex(bv => bv.Token)
            .IsUnique()
            .HasDatabaseName("IX_InvoiceBroadcastVendors_Token");

        builder.HasIndex(bv => bv.InvoiceId)
            .HasDatabaseName("IX_InvoiceBroadcastVendors_InvoiceId");

        builder.HasIndex(bv => bv.IsDeleted)
            .HasDatabaseName("IX_InvoiceBroadcastVendors_IsDeleted");

        builder.HasQueryFilter(bv => !bv.IsDeleted);
    }
}
