using AegisEInvoicing.Domain.Entities.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.UserManagement;

/// <summary>
/// Entity Framework configuration for UserSession entity
/// Configures table structure, relationships, indexes, and constraints for user sessions
/// </summary>
public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("UserSessions");

        builder.HasKey(us => us.Id);

        builder.Property(us => us.Id)
           .IsRequired();

        builder.Property(us => us.UserId)
           .IsRequired();

        builder.Property(us => us.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(us => us.UserAgent)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(us => us.StartedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(us => us.EndedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(us => us.LastActivityAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(us => us.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(us => us.EndReason)
            .HasMaxLength(500);

        builder.Property(us => us.DeviceInfo)
            .HasMaxLength(1000);

        builder.Property(us => us.Location)
            .HasMaxLength(500);

        // Configure foreign key relationship (no navigation property on UserSession)
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes for performance with proper naming
        builder.HasIndex(us => us.UserId)
            .HasDatabaseName("IX_UserSessions_UserId");
        builder.HasIndex(us => us.IsActive)
            .HasDatabaseName("IX_UserSessions_IsActive");
        builder.HasIndex(us => us.StartedAt)
            .HasDatabaseName("IX_UserSessions_StartedAt");
        builder.HasIndex(us => us.LastActivityAt)
            .HasDatabaseName("IX_UserSessions_LastActivityAt");
        builder.HasIndex(us => us.EndedAt)
            .HasDatabaseName("IX_UserSessions_EndedAt");

        // Composite indexes for common queries
        builder.HasIndex(us => new { us.UserId, us.IsActive })
            .HasDatabaseName("IX_UserSessions_UserId_IsActive");
        builder.HasIndex(us => new { us.IsActive, us.LastActivityAt })
            .HasDatabaseName("IX_UserSessions_IsActive_LastActivity");

        // Ignore domain events
        builder.Ignore(us => us.DomainEvents);
    }
}