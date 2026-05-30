using AegisEInvoicing.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

public class AppProviderConfigurationConfiguration : IEntityTypeConfiguration<AppProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<AppProviderConfiguration> builder)
    {
        builder.ToTable("AppProviderConfigurations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        // Lowercase stable key matching IAccessPointProviderClient.ProviderCode
        builder.Property(a => a.AdapterKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.BaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        // Credentials are opaque encrypted blobs; no length constraint
        builder.Property(a => a.EncryptedCredentials)
            .HasColumnType("text");

        builder.Property(a => a.SandboxBaseUrl)
            .HasMaxLength(500);

        builder.Property(a => a.EncryptedSandboxCredentials)
            .HasColumnType("text");

        builder.Property(a => a.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields
        builder.Property(a => a.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.CreatedBy)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.UpdatedBy);

        builder.Property(a => a.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(a => a.DeletedBy);

        builder.Property(a => a.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // One configuration per adapter — AdapterKey is the routing key
        builder.HasIndex(a => a.AdapterKey)
            .IsUnique()
            .HasDatabaseName("IX_AppProviderConfigurations_AdapterKey");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("IX_AppProviderConfigurations_IsActive");

        builder.HasIndex(a => a.IsDeleted)
            .HasDatabaseName("IX_AppProviderConfigurations_IsDeleted");

        builder.HasIndex(a => new { a.IsDeleted, a.IsActive })
            .HasDatabaseName("IX_AppProviderConfigurations_IsDeleted_IsActive");

        // Soft delete filter
        builder.HasQueryFilter(a => !a.IsDeleted);

        builder.Ignore(a => a.DomainEvents);
    }
}
