using AegisEInvoicing.Domain.Entities.VendorManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

public class InvoiceBroadcastConfiguration : IEntityTypeConfiguration<InvoiceBroadcast>
{
    public void Configure(EntityTypeBuilder<InvoiceBroadcast> builder)
    {
        builder.ToTable("InvoiceBroadcasts");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.InvoiceTypeCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(b => b.DueDate)
            .IsRequired();

        builder.Property(b => b.RequiresApproval)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.IsApprovalLocked)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(b => b.Currency)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(b => b.Note)
            .HasMaxLength(1000);

        builder.Property(b => b.BusinessId)
            .IsRequired();

        builder.HasOne(b => b.Business)
            .WithMany()
            .HasForeignKey(b => b.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(b => b.BroadcastVendors)
            .WithOne(bv => bv.InvoiceBroadcast)
            .HasForeignKey(bv => bv.InvoiceBroadcastId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(b => b.BusinessId)
            .HasDatabaseName("IX_InvoiceBroadcasts_BusinessId");

        builder.HasIndex(b => b.Status)
            .HasDatabaseName("IX_InvoiceBroadcasts_Status");

        builder.HasIndex(b => new { b.BusinessId, b.Status })
            .HasDatabaseName("IX_InvoiceBroadcasts_BusinessId_Status");

        builder.HasIndex(b => b.IsDeleted)
            .HasDatabaseName("IX_InvoiceBroadcasts_IsDeleted");

        builder.HasQueryFilter(b => !b.IsDeleted);

        builder.Ignore(b => b.DomainEvents);
    }
}
