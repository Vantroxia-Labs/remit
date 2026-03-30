using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for IntegrationLog entity
/// Configures table structure, indexes, and constraints for integration logs
/// </summary>
public class IntegrationLogConfiguration : IEntityTypeConfiguration<IntegrationLog>
{
    public void Configure(EntityTypeBuilder<IntegrationLog> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("IntegrationLogs");

        builder.HasKey(il => il.Id);

        builder.Property(il => il.Id);

        builder.Property(il => il.Operation)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(il => il.ExternalSystem)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(il => il.RequestData)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(il => il.ResponseData)
            .HasColumnType("text");

        builder.Property(il => il.IsSuccess)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(il => il.ErrorMessage)
            .HasMaxLength(2000);

        builder.Property(il => il.StartedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(il => il.CompletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(il => il.DurationMs);

        builder.Property(il => il.CorrelationId)
            .HasMaxLength(100);

        // Add indexes for performance
        builder.HasIndex(il => il.Operation)
            .HasDatabaseName("IX_IntegrationLogs_Operation");
        builder.HasIndex(il => il.ExternalSystem)
            .HasDatabaseName("IX_IntegrationLogs_ExternalSystem");
        builder.HasIndex(il => il.StartedAt)
            .HasDatabaseName("IX_IntegrationLogs_StartedAt");
        builder.HasIndex(il => il.IsSuccess)
            .HasDatabaseName("IX_IntegrationLogs_IsSuccess");
        builder.HasIndex(il => il.CorrelationId)
            .HasDatabaseName("IX_IntegrationLogs_CorrelationId");

        // Composite indexes for common queries
        builder.HasIndex(il => new { il.ExternalSystem, il.Operation })
            .HasDatabaseName("IX_IntegrationLogs_ExternalSystem_Operation");
        builder.HasIndex(il => new { il.IsSuccess, il.StartedAt })
            .HasDatabaseName("IX_IntegrationLogs_IsSuccess_StartedAt");
        builder.HasIndex(il => new { il.Operation, il.StartedAt })
            .HasDatabaseName("IX_IntegrationLogs_Operation_StartedAt");

        // Ignore domain events
        builder.Ignore(il => il.DomainEvents);
    }
}