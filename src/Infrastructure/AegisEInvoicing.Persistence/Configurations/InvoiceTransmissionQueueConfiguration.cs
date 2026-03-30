using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for InvoiceTransmissionQueue entity
/// Configures table structure, relationships, indexes, and constraints for invoice transmission queue
/// </summary>
public class InvoiceTransmissionQueueConfiguration : IEntityTypeConfiguration<InvoiceTransmissionQueue>
{
    public void Configure(EntityTypeBuilder<InvoiceTransmissionQueue> builder)
    {
        // Table configuration
        builder.ToTable("InvoiceTransmissionQueues");

        // Primary key
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .IsRequired();

        // Properties configuration
        builder.Property(x => x.Irn)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.RequestPayload)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(x => x.BusinessId);

        builder.Property(x => x.UserId);

        builder.Property(x => x.ProcessingStatus)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue(QueueStatus.Pending);

        builder.Property(x => x.AttemptCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.LastErrorMessage)
            .HasMaxLength(1000);

        builder.Property(x => x.ProcessAfter)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CompletedAt)
            .HasColumnType("timestamp with time zone");

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

        // Indexes for performance
        builder.HasIndex(x => x.Irn)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_Irn");

        builder.HasIndex(x => x.Status)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_Status");

        builder.HasIndex(x => x.ProcessingStatus)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_ProcessingStatus");

        builder.HasIndex(x => x.BusinessId)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_BusinessId");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_UserId");

        builder.HasIndex(x => x.ProcessAfter)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_ProcessAfter");

        builder.HasIndex(x => x.CompletedAt)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_CompletedAt");

        builder.HasIndex(x => x.CreatedAt)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_CreatedAt");

        builder.HasIndex(x => x.IsDeleted)
            .HasDatabaseName("IX_InvoiceTransmissionQueues_IsDeleted");

        // Composite indexes for common queries
        builder.HasIndex(x => new { x.ProcessingStatus, x.ProcessAfter })
            .HasDatabaseName("IX_InvoiceTransmissionQueues_ProcessingStatus_ProcessAfter");

        builder.HasIndex(x => new { x.BusinessId, x.ProcessingStatus })
            .HasDatabaseName("IX_InvoiceTransmissionQueues_BusinessId_ProcessingStatus");

        builder.HasIndex(x => new { x.Irn, x.Status })
            .HasDatabaseName("IX_InvoiceTransmissionQueues_Irn_Status");

        builder.HasIndex(x => new { x.IsDeleted, x.ProcessingStatus })
            .HasDatabaseName("IX_InvoiceTransmissionQueues_IsDeleted_ProcessingStatus");

        builder.HasIndex(x => new { x.ProcessingStatus, x.AttemptCount })
            .HasDatabaseName("IX_InvoiceTransmissionQueues_ProcessingStatus_AttemptCount");

        // Add soft delete query filter
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}