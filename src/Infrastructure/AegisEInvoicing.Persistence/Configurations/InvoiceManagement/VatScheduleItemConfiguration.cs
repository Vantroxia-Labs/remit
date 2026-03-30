using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

public class VatScheduleItemConfiguration : IEntityTypeConfiguration<VatScheduleItem>
{
    public void Configure(EntityTypeBuilder<VatScheduleItem> builder)
    {
        builder.ToTable("VatScheduleItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScheduleId).IsRequired();
        builder.Property(x => x.InvoiceId).IsRequired();

        builder.Property(x => x.InvoiceCode)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Irn)
            .HasMaxLength(500);

        builder.Property(x => x.PartyName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.PartyTin)
            .HasMaxLength(50);

        builder.Property(x => x.IssueDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.TaxableAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.VatAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.PaymentStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Ignored computed property
        builder.Ignore(x => x.TotalAmount);

        // An invoice should appear in at most one schedule
        builder.HasIndex(x => new { x.ScheduleId, x.InvoiceId })
            .IsUnique()
            .HasDatabaseName("IX_VatScheduleItems_Schedule_Invoice");

        // Speed up schedule item listing
        builder.HasIndex(x => x.ScheduleId)
            .HasDatabaseName("IX_VatScheduleItems_ScheduleId");
    }
}
