using AegisEInvoicing.Domain.Entities.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.UserManagement;

/// <summary>
/// Entity Framework configuration for PlatformRole entity
/// Configures table structure, relationships, indexes, and constraints for platform roles
/// </summary>
public class PlatformRoleConfiguration : IEntityTypeConfiguration<PlatformRole>
{
    public void Configure(EntityTypeBuilder<PlatformRole> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("PlatformRoles");

        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Id)
            .HasColumnName("Id")
            .IsRequired();

        builder.Property(pr => pr.Name)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(pr => pr.Description)
            .HasColumnName("Description")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(pr => pr.Category)
            .HasColumnName("Category")
            .IsRequired()
            .HasMaxLength(30);

        // Configure Permissions collection
        builder.Property(pr => pr.Permissions)
            .HasConversion(
                permissions => System.Text.Json.JsonSerializer.Serialize(permissions, default(System.Text.Json.JsonSerializerOptions)),
                json => System.Text.Json.JsonSerializer.Deserialize<List<string>>(json, default(System.Text.Json.JsonSerializerOptions)) ?? new List<string>())
            .HasColumnName("permissions")
            .HasColumnType("json");

        builder.Property(pr => pr.SortOrder)
            .HasColumnName("SortOrder")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(pr => pr.IsSystemRole)
            .HasColumnName("IsSystemRole")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pr => pr.IsActive)
            .HasColumnName("IsActive")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(pr => pr.CreatedAt)
            .HasColumnName("CreatedAt")
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(pr => pr.CreatedBy)
            .HasColumnName("CreatedBy")
            .IsRequired();

        builder.Property(pr => pr.UpdatedAt)
            .HasColumnName("UpdatedAt")
            .HasColumnType("timestamp with time zone");

        builder.Property(pr => pr.UpdatedBy)
            .HasColumnName("UpdatedBy");

        builder.Property(pr => pr.DeletedBy)
            .HasColumnName("DeletedBy");

        builder.Property(pr => pr.DeletedAt)
            .HasColumnName("DeletedAt")
            .HasColumnType("timestamp with time zone");

        builder.Property(pr => pr.IsDeleted)
            .HasColumnName("IsDeleted")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(pr => pr.Version)
            .HasColumnName("Version")
            .IsRequired();

        // Null = platform-wide system role; non-null = custom role owned by a business
        builder.Property(pr => pr.BusinessId)
            .HasColumnName("BusinessId")
            .IsRequired(false);

        builder.HasIndex(pr => pr.BusinessId)
            .HasDatabaseName("IX_PlatformRoles_BusinessId");

        // Add indexes for performance
        // Name is unique within scope: globally for system roles, per-business for custom roles.
        // Enforced at the application layer (CreateBusinessRoleCommandHandler checks for duplicates).
        builder.HasIndex(pr => pr.Name)
            .HasDatabaseName("IX_PlatformRoles_Name");
        builder.HasIndex(pr => pr.Category)
            .HasDatabaseName("IX_PlatformRoles_Category");
        builder.HasIndex(pr => pr.SortOrder)
            .HasDatabaseName("IX_PlatformRoles_SortOrder");
        builder.HasIndex(pr => pr.IsActive)
            .HasDatabaseName("IX_PlatformRoles_IsActive");
        builder.HasIndex(pr => pr.IsSystemRole)
            .HasDatabaseName("IX_PlatformRoles_IsSystemRole");
        builder.HasIndex(pr => pr.CreatedAt)
            .HasDatabaseName("IX_PlatformRoles_CreatedAt");
        builder.HasIndex(pr => pr.IsDeleted)
            .HasDatabaseName("IX_PlatformRoles_IsDeleted");

        // Composite indexes
        builder.HasIndex(pr => new { pr.Category, pr.SortOrder })
            .HasDatabaseName("IX_PlatformRoles_Category_SortOrder");
        builder.HasIndex(pr => new { pr.IsDeleted, pr.IsActive })
            .HasDatabaseName("IX_PlatformRoles_IsDeleted_IsActive");

        // Add soft delete query filter
        builder.HasQueryFilter(pr => !pr.IsDeleted);

        // Ignore domain events
        builder.Ignore(pr => pr.DomainEvents);
    }
}