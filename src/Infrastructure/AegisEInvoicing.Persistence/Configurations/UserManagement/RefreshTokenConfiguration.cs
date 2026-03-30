using AegisEInvoicing.Domain.Entities.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.UserManagement;

/// <summary>
/// Entity Framework configuration for RefreshToken entity
/// Configures table structure, relationships, indexes, and constraints for refresh tokens
/// </summary>
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("RefreshTokens");

        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.Id)
           .IsRequired();

        builder.Property(rt => rt.UserId)
            .IsRequired();

        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rt => rt.ExpiresAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500);

        builder.Property(rt => rt.RevokedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(45);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(500);

        builder.Property(rt => rt.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(rt => rt.CreatedByIp)
            .IsRequired()
            .HasMaxLength(45);


        // Configure relationship with User
        builder.HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Add indexes for performance
        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");
        builder.HasIndex(rt => rt.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");
        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");
        builder.HasIndex(rt => rt.RevokedAt)
            .HasDatabaseName("IX_RefreshTokens_RevokedAt");
        builder.HasIndex(rt => rt.CreatedAt)
            .HasDatabaseName("IX_RefreshTokens_CreatedAt");

        // Composite indexes for common queries
        builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_ExpiresAt");
        builder.HasIndex(rt => new { rt.Token, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_Token_ExpiresAt");

        // Ignore domain events
        builder.Ignore(rt => rt.DomainEvents);
    }
}