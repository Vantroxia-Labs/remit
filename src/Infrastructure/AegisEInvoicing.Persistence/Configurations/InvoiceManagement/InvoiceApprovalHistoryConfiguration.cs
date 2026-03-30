using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for InvoiceApprovalHistory entity
/// Configures table structure, relationships, indexes, and constraints for invoice approval history
/// </summary>
public class InvoiceApprovalHistoryConfiguration : IEntityTypeConfiguration<InvoiceApprovalHistory>
{
    public void Configure(EntityTypeBuilder<InvoiceApprovalHistory> builder)
    {
        // Table configuration
        builder.ToTable("InvoiceApprovalHistories");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.InvoiceId)
            .IsRequired();

        builder.Property(x => x.InvoiceStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Comments)
            .IsRequired()
            .HasMaxLength(1000);

        // Audit fields configuration
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

        // Navigation properties
        builder.HasOne(x => x.Invoice)
            .WithMany(i => i.InvoiceApprovalHistory)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("IX_InvoiceApprovalHistories_InvoiceId");

        builder.HasIndex(x => x.InvoiceStatus)
            .HasDatabaseName("IX_InvoiceApprovalHistories_InvoiceStatus");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_InvoiceApprovalHistories_CreatedAt");

        builder.HasIndex(x => new { x.InvoiceId, x.CreatedAt })
            .HasDatabaseName("IX_InvoiceApprovalHistories_InvoiceId_CreatedAt");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName("IX_InvoiceApprovalHistories_IsDeleted");

        // Add soft delete query filter
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}