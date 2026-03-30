using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

/// <summary>
/// Entity Framework configuration for DispatchDocumentReference entity
/// </summary>
public class DispatchDocumentReferenceConfiguration : IEntityTypeConfiguration<InvoiceDispatchDocumentReference>
{
    public void Configure(EntityTypeBuilder<InvoiceDispatchDocumentReference> builder)
    {
        builder.ToTable("DispatchDocumentReferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.IssueDate).IsRequired().HasColumnType("date");

        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(64);
        });

        builder.HasOne(x => x.Invoice)
            .WithOne(i => i.DispatchDocumentReference)
            .HasForeignKey<InvoiceDispatchDocumentReference>(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.InvoiceId)
            .IsUnique()
            .HasDatabaseName("IX_DispatchDocumentReferences_InvoiceId");

        ConfigureAuditFields(builder);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }

    private static void ConfigureAuditFields<T>(EntityTypeBuilder<T> builder) where T : Domain.Common.Implementation.AuditableEntity
    {
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}

/// <summary>
/// Entity Framework configuration for ReceiptDocumentReference entity
/// </summary>
public class ReceiptDocumentReferenceConfiguration : IEntityTypeConfiguration<InvoiceReceiptDocumentReference>
{
    public void Configure(EntityTypeBuilder<InvoiceReceiptDocumentReference> builder)
    {
        builder.ToTable("ReceiptDocumentReferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.IssueDate).IsRequired().HasColumnType("date");

        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(64);
        });

        builder.HasOne(x => x.Invoice)
            .WithOne(i => i.ReceiptDocumentReference)
            .HasForeignKey<InvoiceReceiptDocumentReference>(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.InvoiceId)
            .IsUnique()
            .HasDatabaseName("IX_ReceiptDocumentReferences_InvoiceId");

        ConfigureAuditFields(builder);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }

    private static void ConfigureAuditFields<T>(EntityTypeBuilder<T> builder) where T : Domain.Common.Implementation.AuditableEntity
    {
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}

/// <summary>
/// Entity Framework configuration for OriginatorDocumentReference entity
/// </summary>
public class OriginatorDocumentReferenceConfiguration : IEntityTypeConfiguration<InvoiceOriginatorDocumentReference>
{
    public void Configure(EntityTypeBuilder<InvoiceOriginatorDocumentReference> builder)
    {
        builder.ToTable("OriginatorDocumentReferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.IssueDate).IsRequired().HasColumnType("date");

        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(64);
        });

        builder.HasOne(x => x.Invoice)
            .WithOne(i => i.OriginatorDocumentReference)
            .HasForeignKey<InvoiceOriginatorDocumentReference>(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.InvoiceId)
            .IsUnique()
            .HasDatabaseName("IX_OriginatorDocumentReferences_InvoiceId");

        ConfigureAuditFields(builder);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }

    private static void ConfigureAuditFields<T>(EntityTypeBuilder<T> builder) where T : Domain.Common.Implementation.AuditableEntity
    {
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}

/// <summary>
/// Entity Framework configuration for ContractDocumentReference entity
/// </summary>
public class ContractDocumentReferenceConfiguration : IEntityTypeConfiguration<InvoiceContractDocumentReference>
{
    public void Configure(EntityTypeBuilder<InvoiceContractDocumentReference> builder)
    {
        builder.ToTable("ContractDocumentReferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.IssueDate).IsRequired().HasColumnType("date");

        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(64);
        });

        builder.HasOne(x => x.Invoice)
            .WithOne(i => i.ContractDocumentReference)
            .HasForeignKey<InvoiceContractDocumentReference>(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.InvoiceId)
            .IsUnique()
            .HasDatabaseName("IX_ContractDocumentReferences_InvoiceId");

        ConfigureAuditFields(builder);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }

    private static void ConfigureAuditFields<T>(EntityTypeBuilder<T> builder) where T : Domain.Common.Implementation.AuditableEntity
    {
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}

/// <summary>
/// Entity Framework configuration for AdditionalDocumentReference entity
/// </summary>
public class AdditionalDocumentReferenceConfiguration : IEntityTypeConfiguration<InvoiceAdditionalDocumentReference>
{
    public void Configure(EntityTypeBuilder<InvoiceAdditionalDocumentReference> builder)
    {
        builder.ToTable("AdditionalDocumentReferences");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.InvoiceId).IsRequired();
        builder.Property(x => x.IssueDate).IsRequired().HasColumnType("date");

        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(64);
        });

        builder.HasOne(x => x.Invoice)
            .WithMany(i => i.AdditionalDocumentReferences)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.InvoiceId)
            .HasDatabaseName("IX_AdditionalDocumentReferences_InvoiceId");

        ConfigureAuditFields(builder);
        builder.HasQueryFilter(x => !x.IsDeleted);
    }

    private static void ConfigureAuditFields<T>(EntityTypeBuilder<T> builder) where T : Domain.Common.Implementation.AuditableEntity
    {
        builder.Property(x => x.CreatedAt).IsRequired().HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedBy).IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.DeletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.IsDeleted).IsRequired().HasDefaultValue(false);
    }
}
