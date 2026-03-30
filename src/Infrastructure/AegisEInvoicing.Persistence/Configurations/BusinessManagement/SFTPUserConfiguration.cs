using AegisEInvoicing.Domain.Entities.BusinessManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.BusinessManagement;

/// <summary>
/// Entity Framework configuration for SFTPUser entity
/// </summary>
public class SFTPUserConfiguration : IEntityTypeConfiguration<SFTPUser>
{
    public void Configure(EntityTypeBuilder<SFTPUser> builder)
    {
        builder.HasKey(e => e.Id);

        // Configure Guid properties
        builder.Property(e => e.Id);
        builder.Property(e => e.BusinessId);

        // Configure basic properties
        builder.Property(e => e.Username)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Password)
            .HasMaxLength(500) // Allow for encrypted passwords
            .IsRequired();

        builder.Property(e => e.RootDirectoryPath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.WorkingDirectory)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.DirectoriesCreated)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.SftpInvoiceTransmissionEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure nullable DateTime properties
        builder.Property(e => e.CerberusCreatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.LastSyncedAt)
            .HasColumnType("timestamp with time zone");

        // Configure auditable properties (inherited from AuditableEntity)
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
        builder.HasOne(s => s.Business)
            .WithMany() // Business doesn't have a navigation property for SFTPUsers
            .HasForeignKey(s => s.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => e.BusinessId);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.IsDeleted);

        // Composite index for business and username
        builder.HasIndex(e => new { e.BusinessId, e.Username });

        // Add soft delete query filter
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Table configuration
        builder.ToTable("SFTPUsers");
    }
}