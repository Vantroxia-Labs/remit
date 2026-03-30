using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

/// <summary>
/// Entity Framework configuration for InvoiceItem entity
/// Configures table structure, relationships, indexes, and constraints for invoice items
/// </summary>
public class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        // Table configuration
        builder.ToTable("InvoiceItems");

        // Primary key
        builder.HasKey(x => x.Id);

        // Foreign key properties
        builder.Property(x => x.InvoiceId)
            .IsRequired();

        builder.Property(x => x.BusinessItemId)
            .IsRequired();

        // Basic properties configuration
        builder.Property(x => x.Quantity)
            .IsRequired()
            .HasPrecision(18, 4);

        // Value object configurations
        
        // DiscountFee as owned entity (optional)
        builder.OwnsOne(x => x.DiscountFee, discount =>
        {
            discount.Property(p => p.Amount)
                .HasColumnName("DiscountFee_Amount")
                .HasPrecision(18, 2);

            discount.Property(p => p.Code)
                .HasColumnName("DiscountFee_Code")
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // AdditionalFee as owned entity (optional)
        builder.OwnsOne(x => x.AdditionalFee, fee =>
        {
            fee.Property(p => p.Amount)
                .HasColumnName("AdditionalFee_Amount")
                .HasPrecision(18, 2);

            fee.Property(p => p.Code)
                .HasColumnName("AdditionalFee_Code")
                .HasConversion<string>()
                .HasMaxLength(20);
        });

        // Navigation properties
        builder.HasOne(x => x.Invoice)
            .WithMany(i => i.InvoiceLine)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.BusinessItem)
            .WithMany(bi => bi.InvoiceItems)
            .HasForeignKey(x => x.BusinessItemId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("IX_InvoiceItems_InvoiceId");

        builder.HasIndex(x => x.BusinessItemId)
            .HasDatabaseName("IX_InvoiceItems_BusinessItemId");

        // Composite index - REMOVED .IsUnique() to allow multiple invoice items with same BusinessItemId
        // This enables adding the same business item multiple times to an invoice with different quantities/discounts
        builder.HasIndex(x => new { x.InvoiceId, x.BusinessItemId })
            .HasDatabaseName("IX_InvoiceItems_InvoiceId_BusinessItemId");

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

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}