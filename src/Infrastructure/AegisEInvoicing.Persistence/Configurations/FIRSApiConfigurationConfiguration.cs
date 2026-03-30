using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for FIRSApiConfiguration entity
/// Configures table structure, indexes, and constraints for FIRS API configurations
/// </summary>
public class FIRSApiConfigurationConfiguration : IEntityTypeConfiguration<FIRSApiConfiguration>
{
    public void Configure(EntityTypeBuilder<FIRSApiConfiguration> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("FIRSApiConfigurations");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(f => f.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.DeploymentType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(f => f.EncryptedApiKey)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(f => f.EncryptedApiSecret)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(f => f.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(f => f.IsDefault)
            .IsRequired()
            .HasDefaultValue(false);

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(f => f.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(f => f.CreatedBy)
            .IsRequired();

        builder.Property(f => f.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(f => f.UpdatedBy);

        builder.Property(f => f.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(f => f.DeletedBy);

        builder.Property(f => f.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Add indexes for performance with proper naming
        builder.HasIndex(f => f.Name)
            .IsUnique()
            .HasDatabaseName("IX_FIRSApiConfigurations_Name");
        builder.HasIndex(f => f.DeploymentType)
            .HasDatabaseName("IX_FIRSApiConfigurations_DeploymentType");
        builder.HasIndex(f => f.IsActive)
            .HasDatabaseName("IX_FIRSApiConfigurations_IsActive");
        builder.HasIndex(f => f.IsDefault)
            .HasDatabaseName("IX_FIRSApiConfigurations_IsDefault");
        builder.HasIndex(f => f.CreatedAt)
            .HasDatabaseName("IX_FIRSApiConfigurations_CreatedAt");
        builder.HasIndex(f => f.IsDeleted)
            .HasDatabaseName("IX_FIRSApiConfigurations_IsDeleted");

        // Composite indexes for common queries
        builder.HasIndex(f => new { f.DeploymentType, f.IsActive })
            .HasDatabaseName("IX_FIRSApiConfigurations_Deployment_Active");
        builder.HasIndex(f => new { f.IsActive, f.IsDefault })
            .HasDatabaseName("IX_FIRSApiConfigurations_Active_Default");
        builder.HasIndex(f => new { f.IsDeleted, f.IsActive })
            .HasDatabaseName("IX_FIRSApiConfigurations_IsDeleted_IsActive");

        // Relationship configuration
        builder.HasMany(f => f.BusinessFIRSApiConfigurations)
            .WithOne(bf => bf.FIRSApiConfiguration)
            .HasForeignKey(bf => bf.FIRSApiConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add soft delete query filter
        builder.HasQueryFilter(f => !f.IsDeleted);

        // Ignore domain events
        builder.Ignore(f => f.DomainEvents);
    }
}