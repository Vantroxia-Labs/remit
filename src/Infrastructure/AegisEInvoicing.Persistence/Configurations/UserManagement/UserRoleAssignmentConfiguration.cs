using AegisEInvoicing.Domain.Entities.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.UserManagement;

/// <summary>
/// Entity Framework configuration for UserRoleAssignment entity
/// Configures table structure, relationships, indexes, and constraints for user role assignments
/// </summary>
public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("UserRoleAssignments");

        builder.HasKey(ura => ura.Id);

        builder.Property(ura => ura.Id)
            .IsRequired();

        builder.Property(ura => ura.UserId)
            .IsRequired();

        builder.Property(ura => ura.PlatformRoleId)
            .IsRequired();

        builder.Property(ura => ura.AssignedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(ura => ura.AssignedBy)
            .IsRequired();

        builder.Property(ura => ura.ExpiresAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(ura => ura.RevokedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(ura => ura.RevokedBy);

        builder.Property(ura => ura.RevocationReason)
            .HasMaxLength(500);

        builder.Property(ura => ura.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Configure relationship with User
        builder.HasOne(ura => ura.User)
            .WithMany(u => u.RoleAssignments)
            .HasForeignKey(ura => ura.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with PlatformRole
        builder.HasOne(ura => ura.PlatformRole)
            .WithMany()
            .HasForeignKey(ura => ura.PlatformRoleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Add indexes for performance
        builder.HasIndex(ura => ura.UserId)
            .HasDatabaseName("IX_UserRoleAssignments_UserId");
        builder.HasIndex(ura => ura.PlatformRoleId)
            .HasDatabaseName("IX_UserRoleAssignments_PlatformRoleId");
        builder.HasIndex(ura => new { ura.UserId, ura.PlatformRoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoleAssignments_UserId_PlatformRoleId_Active");
        builder.HasIndex(ura => ura.IsActive)
            .HasDatabaseName("IX_UserRoleAssignments_IsActive");
        builder.HasIndex(ura => ura.AssignedAt)
            .HasDatabaseName("IX_UserRoleAssignments_AssignedAt");
        builder.HasIndex(ura => ura.ExpiresAt)
            .HasDatabaseName("IX_UserRoleAssignments_ExpiresAt");
        builder.HasIndex(ura => ura.RevokedAt)
            .HasDatabaseName("IX_UserRoleAssignments_RevokedAt");

        // Composite indexes for common queries
        builder.HasIndex(ura => new { ura.UserId, ura.IsActive })
            .HasDatabaseName("IX_UserRoleAssignments_UserId_IsActive");
        builder.HasIndex(ura => new { ura.IsActive, ura.ExpiresAt })
            .HasDatabaseName("IX_UserRoleAssignments_IsActive_ExpiresAt");

        // Ignore domain events
        builder.Ignore(ura => ura.DomainEvents);
    }
}