using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

public class InputVatScheduleItemConfiguration : IEntityTypeConfiguration<InputVatScheduleItem>
{
    public void Configure(EntityTypeBuilder<InputVatScheduleItem> builder)
    {
        builder.ToTable("InputVatScheduleItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ScheduleId).IsRequired();
        builder.Property(x => x.ReceivedInvoiceId).IsRequired();

        builder.Property(x => x.Irn)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.SupplierName)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(x => x.SupplierTin)
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

        builder.Ignore(x => x.TotalAmount);

        builder.HasOne(x => x.Schedule)
            .WithMany(x => x.InputItems)
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ScheduleId);
        builder.HasIndex(x => x.ReceivedInvoiceId);
    }
}
