using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.BusinessManagement;

/// <summary>
/// Entity Framework configuration for Party entity
/// Configures table structure, relationships, indexes, and constraints for parties
/// </summary>
public class PartyConfiguration : IEntityTypeConfiguration<Party>
{
    public void Configure(EntityTypeBuilder<Party> builder)
    {
        // Table configuration
        builder.ToTable("Parties");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Phone)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.BusinessID)
            .IsRequired();

        // Value object configurations

        // TIN as owned entity
        builder.OwnsOne(x => x.TaxIdentificationNumber, tin =>
        {
            tin.Property(p => p.Value)
                .HasColumnName("TIN")
                .IsRequired()
                .HasMaxLength(50);

            tin.HasIndex(p => p.Value)
                .HasDatabaseName("IX_Parties_TIN");
        });

        // Address as owned entity
        builder.OwnsOne(x => x.Address, address =>
        {
            address.Property(p => p.Street)
                .HasColumnName("Street")
                .IsRequired()
                .HasMaxLength(200);

            address.Property(p => p.City)
                .HasColumnName("City")
                .IsRequired()
                .HasMaxLength(100);

            address.Property(p => p.State)
                .HasColumnName("State")
                .IsRequired()
                .HasMaxLength(100);

            address.Property(p => p.Country)
                .HasColumnName("Country")
                .IsRequired()
                .HasMaxLength(100);

            address.Property(p => p.PostalCode)
                .HasColumnName("PostalCode")
                .HasMaxLength(20);
        });

        // Navigation properties
        builder.HasOne(x => x.Business)
            .WithMany(b => b.Parties)
            .HasForeignKey(x => x.BusinessID)
            .OnDelete(DeleteBehavior.Restrict);

        // One-to-many relationship with Invoices
        builder.HasMany(x => x.Invoices)
            .WithOne(i => i.Party)
            .HasForeignKey(i => i.PartyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.BusinessID)
            .HasDatabaseName("IX_Parties_BusinessId");

        builder.HasIndex(x => x.Email)
            .HasDatabaseName("IX_Parties_Email");

        builder.HasIndex(x => x.Name)
            .HasDatabaseName("IX_Parties_Name");

        // Composite indexes for common queries
        builder.HasIndex(x => new { x.BusinessID, x.Email })
            .IsUnique()
            .HasDatabaseName("IX_Parties_BusinessId_Email");

        builder.HasIndex(x => new { x.BusinessID, x.Name })
            .HasDatabaseName("IX_Parties_BusinessId_Name");

        // Audit fields configuration (inherited from AuditableEntity)
        builder.Property(x => x.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.UpdatedBy);

        builder.Property(x => x.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.DeletedBy);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Add soft delete query filter
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Ignore domain events
        builder.Ignore(x => x.DomainEvents);
    }
}