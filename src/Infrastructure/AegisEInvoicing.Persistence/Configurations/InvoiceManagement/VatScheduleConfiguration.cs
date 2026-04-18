using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

public class VatScheduleConfiguration : IEntityTypeConfiguration<VatSchedule>
{
    public void Configure(EntityTypeBuilder<VatSchedule> builder)
    {
        builder.ToTable("VatSchedules");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Year).IsRequired();
        builder.Property(x => x.Month).IsRequired();

        builder.Property(x => x.MonthName)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.PeriodStart)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.PeriodEnd)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.DueDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.FiledAt)
            .HasColumnType("timestamptz");

        builder.Property(x => x.TotalInvoiceCount).IsRequired();

        builder.Property(x => x.TotalTaxableAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.TotalVatAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.TotalInputInvoiceCount).IsRequired();

        builder.Property(x => x.TotalInputTaxableAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.TotalInputVatAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Ignore(x => x.NetVatPayable);

        builder.Property(x => x.BusinessId).IsRequired();

        // One schedule per business per month — enforced at the DB level
        builder.HasIndex(x => new { x.BusinessId, x.Year, x.Month })
            .IsUnique()
            .HasDatabaseName("IX_VatSchedules_Business_Period");

        // Navigation
        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Schedule)
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.InputItems)
            .WithOne(x => x.Schedule)
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
