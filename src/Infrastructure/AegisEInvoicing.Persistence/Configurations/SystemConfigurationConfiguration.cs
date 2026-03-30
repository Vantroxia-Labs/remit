using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for SystemConfiguration entity
/// Configures table structure, indexes, and constraints for system configurations
/// </summary>
public class SystemConfigurationConfiguration : IEntityTypeConfiguration<SystemConfiguration>
{
    public void Configure(EntityTypeBuilder<SystemConfiguration> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("SystemConfigurations");

        builder.HasKey(sc => sc.Id);

        builder.Property(sc => sc.Id)
            .IsRequired();

        builder.Property(sc => sc.OrganizationName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(sc => sc.DeploymentMode)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(sc => sc.IsSetupCompleted)
            .IsRequired();

        builder.Property(sc => sc.SetupCompletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(sc => sc.SetupCompletedBy);

        builder.Property(sc => sc.LicenseKey)
            .HasMaxLength(500);

        builder.Property(sc => sc.LicenseExpiryDate)
            .HasColumnType("timestamp with time zone");

        builder.Property(sc => sc.OrganizationContactEmail)
            .HasMaxLength(255);

        builder.Property(sc => sc.OrganizationContactPhone)
            .HasMaxLength(50);

        builder.Property(sc => sc.AllowSelfOnboarding)
            .IsRequired();

        builder.Property(sc => sc.MaxBusinessesAllowed)
            .IsRequired();

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(sc => sc.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(sc => sc.CreatedBy)
            .IsRequired();

        builder.Property(sc => sc.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(sc => sc.UpdatedBy);

        builder.Property(sc => sc.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(sc => sc.DeletedBy);

        builder.Property(sc => sc.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sc => sc.Version)
            .IsRequired()
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(sc => sc.DeploymentMode)
            .HasDatabaseName("ix_system_configurations_deployment_mode");

        builder.HasIndex(sc => sc.IsSetupCompleted)
            .HasDatabaseName("ix_system_configurations_is_setup_completed");

        builder.HasIndex(sc => sc.IsDeleted)
            .HasDatabaseName("IX_SystemConfigurations_IsDeleted");

        // There should only be one system configuration record
        builder.HasIndex(sc => sc.Id)
            .IsUnique()
            .HasDatabaseName("IX_SystemConfigurations_Unique");

        // Add soft delete query filter
        builder.HasQueryFilter(sc => !sc.IsDeleted);

        // Ignore domain events
        builder.Ignore(sc => sc.DomainEvents);
    }
}