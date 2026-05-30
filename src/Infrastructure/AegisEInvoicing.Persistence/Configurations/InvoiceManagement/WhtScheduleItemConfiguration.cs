using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

public class WhtScheduleItemConfiguration : IEntityTypeConfiguration<WhtScheduleItem>
{
    public void Configure(EntityTypeBuilder<WhtScheduleItem> builder)
    {
        builder.ToTable("WhtScheduleItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScheduleId).IsRequired();
        builder.Property(x => x.ReceivedInvoiceId).IsRequired();

        builder.Property(x => x.VendorName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.VendorAddress)
            .HasMaxLength(500);

        builder.Property(x => x.VendorTin)
            .HasMaxLength(50);

        builder.Property(x => x.Irn)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IssueDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.NatureOfTransaction)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.GrossAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.WhtRate)
            .IsRequired()
            .HasColumnType("numeric(5,2)");

        builder.Property(x => x.WhtAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.TaxAuthority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        // Computed property — not persisted
        builder.Ignore(x => x.NetAmount);

        // Each received invoice appears in at most one WHT schedule
        builder.HasIndex(x => new { x.ScheduleId, x.ReceivedInvoiceId })
            .IsUnique()
            .HasDatabaseName("IX_WhtScheduleItems_Schedule_ReceivedInvoice");

        builder.HasIndex(x => x.ScheduleId)
            .HasDatabaseName("IX_WhtScheduleItems_ScheduleId");
    }
}
