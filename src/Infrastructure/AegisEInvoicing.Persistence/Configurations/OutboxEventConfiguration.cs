using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for OutboxEvent entity
/// Configures table structure, indexes, and constraints for outbox events
/// </summary>
public class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEvent>
{
    public void Configure(EntityTypeBuilder<OutboxEvent> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("OutboxEvents");

        builder.HasKey(oe => oe.Id);

        builder.Property(oe => oe.Id);

        builder.Property(oe => oe.EventType)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(oe => oe.EventData)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(oe => oe.OccurredOnUtc)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(oe => oe.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(oe => oe.ProcessedOnUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(oe => oe.Error)
            .HasMaxLength(2000);

        builder.Property(oe => oe.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(oe => oe.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        // Add indexes for performance
        builder.HasIndex(oe => oe.Status)
            .HasDatabaseName("IX_OutboxEvents_Status");
        builder.HasIndex(oe => oe.CreatedAt)
            .HasDatabaseName("IX_OutboxEvents_CreatedAt");
        builder.HasIndex(oe => oe.ProcessedOnUtc)
            .HasDatabaseName("IX_OutboxEvents_ProcessedOnUtc");
        builder.HasIndex(oe => oe.OccurredOnUtc)
            .HasDatabaseName("IX_OutboxEvents_OccurredOnUtc");

        // Composite indexes for common queries
        builder.HasIndex(oe => new { oe.Status, oe.CreatedAt })
            .HasDatabaseName("IX_OutboxEvents_Status_CreatedAt");
        builder.HasIndex(oe => new { oe.Status, oe.RetryCount })
            .HasDatabaseName("IX_OutboxEvents_Status_RetryCount");

        // Ignore domain events if applicable
        builder.Ignore(oe => oe.DomainEvents);
    }
}