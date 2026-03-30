using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ApiUsageTracking entity
/// Configures table structure, relationships, indexes, and constraints for API usage tracking
/// </summary>
public class ApiUsageTrackingConfiguration : IEntityTypeConfiguration<ApiUsageTracking>
{
    public void Configure(EntityTypeBuilder<ApiUsageTracking> builder)
    {
        // Table configuration
        builder.ToTable("ApiUsageTrackings");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Since we use Guid.CreateVersion7()

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.UserId);

        builder.Property(x => x.Endpoint)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.ResponseStatusCode)
            .IsRequired();

        builder.Property(x => x.ResponseTimeMs)
            .IsRequired();

        builder.Property(x => x.RequestSizeBytes)
            .IsRequired();

        builder.Property(x => x.ResponseSizeBytes)
            .IsRequired();

        builder.Property(x => x.RequestTimestamp)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(x => x.UserAgent)
            .HasMaxLength(1000);

        builder.Property(x => x.ApiKeyUsed)
            .HasMaxLength(100);

        builder.Property(x => x.IsBillable)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Cost)
            .HasPrecision(18, 4);

        builder.Property(x => x.FIRSInvoiceId)
            .HasMaxLength(100);

        builder.Property(x => x.UsedAegisCredentials)
            .IsRequired()
            .HasDefaultValue(false);

        // Audit fields configuration
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedBy);

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
        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(x => x.BusinessId)
            .HasDatabaseName("IX_ApiUsageTrackings_BusinessId");

        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_ApiUsageTrackings_UserId");

        builder.HasIndex(x => x.RequestTimestamp)
            .HasDatabaseName("IX_ApiUsageTrackings_RequestTimestamp");

        builder.HasIndex(x => x.Endpoint)
            .HasDatabaseName("IX_ApiUsageTrackings_Endpoint");

        builder.HasIndex(x => x.IsBillable)
            .HasDatabaseName("IX_ApiUsageTrackings_IsBillable");

        builder.HasIndex(x => new { x.BusinessId, x.RequestTimestamp })
            .HasDatabaseName("IX_ApiUsageTrackings_BusinessId_RequestTimestamp");

        builder.HasIndex(x => new { x.BusinessId, x.IsBillable })
            .HasDatabaseName("IX_ApiUsageTrackings_BusinessId_IsBillable");

        // Query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}

/// <summary>
/// Entity Framework configuration for ApiUsageSummary entity
/// Configures table structure, relationships, indexes, and constraints for API usage summaries
/// </summary>
public class ApiUsageSummaryConfiguration : IEntityTypeConfiguration<ApiUsageSummary>
{
    public void Configure(EntityTypeBuilder<ApiUsageSummary> builder)
    {
        // Table configuration
        builder.ToTable("ApiUsageSummaries");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.Id)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.PeriodStart)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PeriodEnd)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.TotalRequests)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.SuccessfulRequests)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.FailedRequests)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalDataTransferredBytes)
            .IsRequired()
            .HasDefaultValue(0L);

        builder.Property(x => x.TotalCost)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        // JSON columns for dictionaries
        builder.Property(x => x.EndpointUsage)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, int>())
            .HasColumnType("text")
            .HasColumnName("EndpointUsage");

        builder.Property(x => x.EndpointCosts)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, decimal>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, decimal>())
            .HasColumnType("text")
            .HasColumnName("EndpointCosts");

        builder.Property(x => x.FIRSOperationsCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.FIRSOperationsCost)
            .IsRequired()
            .HasPrecision(18, 4)
            .HasDefaultValue(0m);

        builder.Property(x => x.IsFinalized)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.FinalizedAt)
            .HasColumnType("timestamp with time zone");

        // Audit fields configuration
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedBy);

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
        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.BusinessId)
            .HasDatabaseName("IX_ApiUsageSummaries_BusinessId");

        builder.HasIndex(x => x.PeriodStart)
            .HasDatabaseName("IX_ApiUsageSummaries_PeriodStart");

        builder.HasIndex(x => x.PeriodEnd)
            .HasDatabaseName("IX_ApiUsageSummaries_PeriodEnd");

        builder.HasIndex(x => x.IsFinalized)
            .HasDatabaseName("IX_ApiUsageSummaries_IsFinalized");

        builder.HasIndex(x => new { x.BusinessId, x.PeriodStart, x.PeriodEnd })
            .HasDatabaseName("IX_ApiUsageSummaries_BusinessId_Period")
            .IsUnique();

        // Query filter for soft delete
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}