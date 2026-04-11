using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

public class AppProviderConfigurationConfiguration : IEntityTypeConfiguration<AppProviderConfiguration>
{
    public void Configure(EntityTypeBuilder<AppProviderConfiguration> builder)
    {
        builder.ToTable("AppProviderConfigurations");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.ProviderCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(a => a.AuthScheme)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.ApiKeyHeaderName)
            .HasMaxLength(100);

        builder.Property(a => a.SignatureHeaderName)
            .HasMaxLength(100);

        // Sandbox credentials
        builder.Property(a => a.SandboxBaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.EncryptedSandboxApiKey)
            .HasColumnType("text");

        builder.Property(a => a.EncryptedSandboxApiSecret)
            .HasColumnType("text");

        builder.Property(a => a.SandboxTokenEndpoint)
            .HasMaxLength(500);

        // Production credentials
        builder.Property(a => a.ProductionBaseUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.EncryptedProductionApiKey)
            .HasColumnType("text");

        builder.Property(a => a.EncryptedProductionApiSecret)
            .HasColumnType("text");

        builder.Property(a => a.ProductionTokenEndpoint)
            .HasMaxLength(500);

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

        // ProviderCode must be unique — it is the routing key
        builder.HasIndex(a => a.ProviderCode)
            .IsUnique()
            .HasDatabaseName("IX_AppProviderConfigurations_ProviderCode");

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
