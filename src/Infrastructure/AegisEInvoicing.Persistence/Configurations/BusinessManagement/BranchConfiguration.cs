using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Branch entity
/// </summary>
public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.HasKey(e => e.Id);

        // Configure Guid properties
        builder.Property(e => e.Id);

        builder.Property(e => e.BusinessId)
            .IsRequired();

        builder.Property(e => e.AdminUserId);

        // Configure auditable properties (inherited from AuditableEntity)
        builder.Property(e => e.CreatedBy);

        builder.Property(e => e.UpdatedBy);

        builder.Property(e => e.DeletedBy);

        // Configure Address as owned type
        builder.OwnsOne(e => e.Address, address =>
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

        // Configure other properties
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Code)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ContactEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.ContactPhone)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        builder.Property(e => e.IsHeadOffice)
            .HasDefaultValue(false);

        // Audit fields configuration (inherited from AuditableEntity)
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

        // Indexes
        builder.HasIndex(e => e.BusinessId);
        builder.HasIndex(e => e.Code);
        builder.HasIndex(e => e.IsActive);
        builder.HasIndex(e => e.IsDeleted);

        // Relationships
        builder.HasOne(e => e.Business)
            .WithMany(b => b.Branches)
            .HasForeignKey(e => e.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AdminUser)
            .WithMany()
            .HasForeignKey(e => e.AdminUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Users)
            .WithOne(u => u.Branch)
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraints
        builder.HasIndex(e => new { e.BusinessId, e.Code })
            .IsUnique();

        // Additional indexes
        builder.HasIndex(e => e.AdminUserId);

        // Add soft delete query filter
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);

        // Table configuration - Use PascalCase plural naming
        builder.ToTable("Branches");
    }
}