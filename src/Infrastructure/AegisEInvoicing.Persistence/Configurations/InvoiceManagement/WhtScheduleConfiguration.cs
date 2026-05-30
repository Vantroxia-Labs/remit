using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

public class WhtScheduleConfiguration : IEntityTypeConfiguration<WhtSchedule>
{
    public void Configure(EntityTypeBuilder<WhtSchedule> builder)
    {
        builder.ToTable("WhtSchedules");

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

        builder.Property(x => x.TotalItemCount).IsRequired();

        builder.Property(x => x.TotalGrossAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.TotalWhtAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.TotalNrsWhtAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.TotalStateWhtAmount)
            .IsRequired()
            .HasColumnType("numeric(18,2)");

        builder.Property(x => x.BusinessId).IsRequired();

        // One WHT schedule per business per month
        builder.HasIndex(x => new { x.BusinessId, x.Year, x.Month })
            .IsUnique()
            .HasDatabaseName("IX_WhtSchedules_Business_Period");

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Schedule)
            .HasForeignKey(x => x.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
