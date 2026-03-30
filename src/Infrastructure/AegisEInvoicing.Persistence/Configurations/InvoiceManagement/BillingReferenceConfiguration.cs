using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

/// <summary>
/// Entity Framework configuration for BillingReference entity
/// Configures table structure, relationships, indexes, and constraints for billing references
/// </summary>
public class BillingReferenceConfiguration : IEntityTypeConfiguration<InvoiceBillingReference>
{
    public void Configure(EntityTypeBuilder<InvoiceBillingReference> builder)
    {
        // Table configuration
        builder.ToTable("BillingReferences");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.InvoiceId)
            .IsRequired();

        builder.Property(x => x.IssueDate)
            .IsRequired()
            .HasColumnType("date");

        // IRN as owned entity (value object)
        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(64);
        });

        // Navigation properties
        builder.HasOne(x => x.Invoice)
            .WithMany(i => i.BillingReferences)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade); // When an invoice is deleted, delete its billing references

        // Indexes
        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("IX_BillingReferences_InvoiceId");

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedBy);

        builder.Property(x => x.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DeletedBy);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Add soft delete query filter
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
