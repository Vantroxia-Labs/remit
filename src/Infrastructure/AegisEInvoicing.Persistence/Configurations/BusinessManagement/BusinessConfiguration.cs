using AegisEInvoicing.Domain.Entities.BusinessManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Business entity
/// </summary>
public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.HasKey(e => e.Id);

        // Configure Guid properties
        builder.Property(e => e.Id);

        builder.Property(e => e.AdminUserId);

        // Configure FlowRuleId as nullable Guid (for backward compatibility)
        builder.Property(e => e.FlowRuleId);

        // Configure BusinessFIRSApiConfigurationId as nullable Guid
        builder.Property(e => e.BusinessFIRSApiConfigurationId);

        // Configure auditable properties (inherited from AuditableAggregateRoot)
        builder.Property(e => e.CreatedBy);

        builder.Property(e => e.UpdatedBy);

        builder.Property(e => e.DeletedBy);

        // Configure Address as owned type
        builder.OwnsOne(e => e.RegisteredAddress, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("address_street")
                .HasMaxLength(200)
                .IsRequired();

            address.Property(a => a.City)
                .HasColumnName("address_city")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.State)
                .HasColumnName("address_state")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.Country)
                .HasColumnName("address_country")
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.PostalCode)
                .HasColumnName("address_postal_code")
                .HasMaxLength(20)
                .IsRequired();
        });

        // Configure TIN as owned type
        builder.OwnsOne(e => e.TaxIdentificationNumber, tin =>
        {
            tin.Property(t => t.Value)
                .HasColumnName("tax_identification_number")
                .HasMaxLength(50)
                .IsRequired();
        });

        // Configure basic properties
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.BusinessRegistrationNumber)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ContactEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.ContactPhone)
            .HasMaxLength(50);

        builder.Property(e => e.ServiceId)
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.OAuth2Token)
            .HasMaxLength(2000);

        builder.Property(e => e.FIRSBusinessId)
           .IsRequired();

        builder.Property(e => e.PublicKey)
            .HasColumnType("text");

        builder.Property(e => e.Certificate)
           .HasColumnType("text");

        builder.Property(e => e.Industry)
           .HasMaxLength(255)
           .IsRequired();

        builder.Property(e => e.FIRSApiKey)
            .HasColumnType("text");

        builder.Property(e => e.FIRSClientSecret)
            .HasColumnType("text");

        // API Key properties
        builder.Property(e => e.ApiKey)
            .HasMaxLength(500);

        builder.Property(e => e.ApiKeyGeneratedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.ApiKeyLastUsedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.IsApiKeyActive)
            .HasDefaultValue(false);

        // Token properties
        builder.Property(e => e.TokenExpiresAt)
            .HasColumnType("timestamp with time zone");

        // Audit fields configuration (inherited from AuditableAggregateRoot)
        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.CreatedBy)
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UpdatedBy);

        builder.Property(e => e.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.DeletedBy);

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships
        // Admin User relationship - uses AdminUserId foreign key on Business table
        builder.HasOne(b => b.AdminUser)
            .WithMany() // No navigation property back to Business for admin relationship
            .HasForeignKey(b => b.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false); // Make the foreign key optional

        // Subscription relationship — one business can have many subscriptions (one per plan tier)
        builder.HasMany(b => b.Subscriptions)
            .WithOne(s => s.Business)
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // FlowRules relationship - One Business can have many FlowRules
        builder.HasMany(b => b.FlowRules)
            .WithOne(fr => fr.Business)
            .HasForeignKey(fr => fr.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Branches relationship
        builder.HasMany(b => b.Branches)
            .WithOne(br => br.Business)
            .HasForeignKey(br => br.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Users collection relationship is configured in UserConfiguration
        // No need to configure it here as it would create a duplicate relationship

        // BusinessFIRSApiConfiguration relationship - One-to-One
        builder.HasOne(b => b.BusinessFIRSApiConfiguration)
            .WithOne(bf => bf.Business)
            .HasForeignKey<BusinessFIRSApiConfiguration>(bf => bf.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // APP Provider switching — nullable string, matches IAccessPointProviderClient.ProviderCode
        builder.Property(e => e.ActiveAdapterKey)
            .HasMaxLength(100);

        builder.Property(e => e.AppEnvironmentMode)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(AegisEInvoicing.Domain.Enums.AppEnvironmentMode.Sandbox);

        // Indexes
        builder.HasIndex(e => e.Name).IsUnique();
        builder.HasIndex(e => e.BusinessRegistrationNumber).IsUnique();

        builder.HasIndex(e => e.ServiceId).IsUnique();
        builder.HasIndex(e => e.FIRSBusinessId).IsUnique();
        builder.HasIndex(e => e.ContactEmail);
        builder.HasIndex(e => e.AdminUserId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsDeleted);

        // Add soft delete query filter
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);

        // Table configuration - Use PascalCase plural naming
        builder.ToTable("Businesses");
    }
}