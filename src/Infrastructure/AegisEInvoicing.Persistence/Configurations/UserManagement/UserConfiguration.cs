using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.UserManagement;

/// <summary>
/// Entity Framework configuration for User entity
/// Configures table structure, relationships, indexes, and constraints for users
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .IsRequired();

        builder.Property(u => u.BusinessId);

        builder.Property(u => u.BranchId);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .UseCollation("case_insensitive"); // PostgreSQL case-insensitive collation

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50);

        // Configure PasswordHash value object
        builder.OwnsOne(u => u.PasswordHash, ph =>
        {
            ph.Property(p => p.Hash)
                .IsRequired()
                .HasMaxLength(255);

            ph.Property(p => p.Salt)
                .IsRequired()
                .HasMaxLength(255);

            ph.Property(p => p.CreatedAt)
                .HasColumnName("PasswordCreatedAt")
                .IsRequired();
        });

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(u => u.IsEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.MustChangePassword)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.PasswordChangedAt);

        builder.Property(u => u.LastLoginAt);

        builder.Property(u => u.FailedLoginAttempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(u => u.LockedOutUntil);

        // Configure Aegis-specific properties
        builder.Property(u => u.IsAegisUser)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.AegisRole)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.AegisEmployeeId)
            .HasMaxLength(50);

        builder.Property(u => u.AegisDepartment)
            .HasMaxLength(100);

        builder.Property(u => u.LastAegisActivityAt);

        // Configure UserPreferences value object
        builder.OwnsOne(u => u.Preferences, p =>
        {
            p.Property(pr => pr.Language)
                .HasMaxLength(10)
                .HasDefaultValue("en-US");

            p.Property(pr => pr.TimeZone)
                .HasMaxLength(100)
                .HasDefaultValue("UTC");

            p.Property(pr => pr.DateFormat)
                .HasMaxLength(50)
                .HasDefaultValue("yyyy-MM-dd");

            p.Property(pr => pr.NumberFormat)
                .HasMaxLength(10)
                .HasDefaultValue("en-US");

            p.Property(pr => pr.EmailNotifications)
                .HasDefaultValue(true);

            p.Property(pr => pr.SmsNotifications)
                .HasDefaultValue(false);

            p.Property(pr => pr.InvoiceReminders)
                .HasDefaultValue(true);

            p.Property(pr => pr.SecurityAlerts)
                .HasDefaultValue(true);

            p.Property(pr => pr.Theme)
                .HasMaxLength(20)
                .HasDefaultValue("light");

            p.Property(pr => pr.PageSize)
                .HasDefaultValue(25);

            p.Property(pr => pr.TwoFactorEnabled)
                .HasDefaultValue(false);
        });

        // Configure one-to-many relationship with role assignments
        builder.HasMany(u => u.RoleAssignments)
            .WithOne(ra => ra.User)
            .HasForeignKey(ra => ra.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure relationship with Business
        builder.HasOne(u => u.Business)
            .WithMany(m => m.Users)
            .HasForeignKey(u => u.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure relationship with Branch
        builder.HasOne(u => u.Branch)
            .WithMany(mb => mb.Users)
            .HasForeignKey(u => u.BranchId)
            .OnDelete(DeleteBehavior.SetNull);

        // Add indexes for performance
        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.BusinessId);
        builder.HasIndex(u => u.BranchId);
        builder.HasIndex(u => u.Status);
        builder.HasIndex(u => u.CreatedAt);
        builder.HasIndex(u => u.LastLoginAt);

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.CreatedBy)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.UpdatedBy);

        builder.Property(u => u.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.DeletedBy);

        builder.Property(u => u.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.Version)
            .IsRequired()
            .HasDefaultValue(0);

        // Timestamp fields with proper column types
        builder.Property(u => u.PasswordChangedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.LastLoginAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.LockedOutUntil)
            .HasColumnType("timestamp with time zone");

        builder.Property(u => u.LastAegisActivityAt)
            .HasColumnType("timestamp with time zone");

        // Add soft delete query filter
        builder.HasQueryFilter(u => !u.IsDeleted);

        // Additional indexes for audit and soft delete
        builder.HasIndex(u => u.IsDeleted);
        builder.HasIndex(u => new { u.IsDeleted, u.Status });
        
        // Ignore domain events
        builder.Ignore(u => u.DomainEvents);
    }
}