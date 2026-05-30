using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

public class InvoiceDraftConfiguration : IEntityTypeConfiguration<InvoiceDraft>
{
    public void Configure(EntityTypeBuilder<InvoiceDraft> builder)
    {
        builder.ToTable("InvoiceDrafts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.IssueDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.PartyName)
            .HasMaxLength(500);

        builder.Property(x => x.DraftPayload)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.IsDeleted).HasDefaultValue(false);

        builder.HasIndex(x => x.BusinessId)
            .HasDatabaseName("IX_InvoiceDrafts_BusinessId");

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
