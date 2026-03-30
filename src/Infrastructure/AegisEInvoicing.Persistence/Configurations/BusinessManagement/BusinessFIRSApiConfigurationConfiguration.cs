using AegisEInvoicing.Domain.Entities.BusinessManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.BusinessManagement;

/// <summary>
/// Entity Framework configuration for BusinessFIRSApiConfiguration entity
/// Configures the join table for the many-to-many relationship between Business and FIRSApiConfiguration
/// </summary>
public class BusinessFIRSApiConfigurationConfiguration : IEntityTypeConfiguration<BusinessFIRSApiConfiguration>
{
    public void Configure(EntityTypeBuilder<BusinessFIRSApiConfiguration> builder)
    {
        // Table configuration
        builder.ToTable("BusinessFIRSApiConfigurations");

        builder.HasKey(bf => bf.Id);

        builder.Property(bf => bf.Id)
            .IsRequired();

        builder.Property(bf => bf.BusinessId)
            .IsRequired();

        builder.Property(bf => bf.FIRSApiConfigurationId)
            .IsRequired();

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(bf => bf.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(bf => bf.CreatedBy)
            .IsRequired();

        builder.Property(bf => bf.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(bf => bf.UpdatedBy);

        builder.Property(bf => bf.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(bf => bf.DeletedBy);

        builder.Property(bf => bf.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationship configurations
        // Business relationship is configured in BusinessConfiguration
        builder.HasOne(bf => bf.Business)
            .WithOne(b => b.BusinessFIRSApiConfiguration)
            .HasForeignKey<BusinessFIRSApiConfiguration>(bf => bf.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // FIRSApiConfiguration relationship is configured in FIRSApiConfigurationConfiguration
        builder.HasOne(bf => bf.FIRSApiConfiguration)
            .WithMany(f => f.BusinessFIRSApiConfigurations)
            .HasForeignKey(bf => bf.FIRSApiConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for performance
        builder.HasIndex(bf => bf.BusinessId)
            .HasDatabaseName("IX_BusinessFIRSApiConfigurations_BusinessId");

        builder.HasIndex(bf => bf.FIRSApiConfigurationId)
            .HasDatabaseName("IX_BusinessFIRSApiConfigurations_FIRSApiConfigurationId");

        builder.HasIndex(bf => new { bf.BusinessId, bf.FIRSApiConfigurationId })
            .IsUnique()
            .HasDatabaseName("IX_BusinessFIRSApiConfigurations_Business_FIRSConfig");

        builder.HasIndex(bf => bf.CreatedAt)
            .HasDatabaseName("IX_BusinessFIRSApiConfigurations_CreatedAt");

        builder.HasIndex(bf => bf.IsDeleted)
            .HasDatabaseName("IX_BusinessFIRSApiConfigurations_IsDeleted");

        // Composite index for common queries
        builder.HasIndex(bf => new { bf.IsDeleted, bf.BusinessId })
            .HasDatabaseName("IX_BusinessFIRSApiConfigurations_IsDeleted_BusinessId");

        // Add soft delete query filter
        builder.HasQueryFilter(bf => !bf.IsDeleted);

        // Ignore domain events
        builder.Ignore(bf => bf.DomainEvents);
    }
}