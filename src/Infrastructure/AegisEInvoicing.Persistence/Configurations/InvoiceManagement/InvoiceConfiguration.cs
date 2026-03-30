using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.FIRSAccessPoint.Models.Enumerators;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

/// <summary>
/// Entity Framework configuration for Invoice entity
/// Configures table structure, relationships, indexes, and constraints for invoices
/// </summary>
public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        // Table configuration
        builder.ToTable("Invoices");

        // Primary key
        builder.HasKey(x => x.Id);

        // Properties configuration
        builder.Property(x => x.InvoiceCode)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.BusinessId)
            .IsRequired();

        builder.Property(x => x.IssueDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.DueDate)
            .HasColumnType("date");
        
        builder.Property(x => x.IssueTime)
            .HasColumnType("time");


        builder.OwnsOne(x => x.InvoiceType, invoiceType =>
        {
            invoiceType.Property(p => p.Code)
                .HasColumnName("InvoiceType_Code")
                .IsRequired();

            invoiceType.Property(p => p.Name)
                .HasColumnName("InvoiceType_Name")
                .IsRequired()
                .HasMaxLength(500);
        });

        builder.Property(x => x.Note)
            .HasColumnType("text");     


        builder.OwnsOne(x => x.Currency, currency =>
        {
            currency.Property(p => p.Code)
                .HasColumnName("Currency_Code")
                .IsRequired()
                .HasMaxLength(4);

            currency.Property(p => p.Name)
                .HasColumnName("Currency_Name")
                .IsRequired()
                .HasMaxLength(500);
        });

        builder.Property(x => x.FIRSSubmissionId)
            .HasMaxLength(500);

        builder.Property(x => x.FIRSSubmissionResponseMessage)
           .HasColumnType("text");

        builder.Property(x => x.SubmittedToFIRSAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PaymentStatus)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PaymentStatus.Pending);

        builder.Property(x => x.PaymentTerms)
            .HasColumnType("text");

        builder.Property(x => x.PaymentReference)
            .HasColumnType("text");

        builder.Property(x => x.InvoiceStatus)
           .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.InvoiceKind)
            .HasConversion<string?>()
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(x => x.InvoiceSource)
          .HasConversion<string>()
           .HasMaxLength(20);

        // Value object configurations

        // IRN as owned entity
        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(64);

            irn.HasIndex(p => p.Value)
                .IsUnique()
                .HasDatabaseName("IX_Invoices_IRN");
        });

        // Invoice Delivery Period as owned entity
        builder.OwnsOne(x => x.DeliveryPeriod, period =>
        {
            period.Property(p => p.StartDate)
                .HasColumnName("DeliveryPeriod_StartDate")
                .IsRequired()
                .HasColumnType("date");

            period.Property(p => p.EndDate)
                .HasColumnName("DeliveryPeriod_EndDate")
                .IsRequired()
                .HasColumnType("date");
        });

        // QR Code as owned entity
        builder.OwnsOne(x => x.QRCode, qr =>
        {
            qr.Property(p => p.EncryptedData)
                .HasColumnName("QRCode_EncryptedData")
                .HasColumnType("text");

            qr.Property(p => p.Base64Image)
              .HasColumnName("QRCode_Base64Image")
              .HasColumnType("text");

            qr.Property(p => p.GeneratedAt)
                .HasColumnName("QRCode_GeneratedAt")
                .HasColumnType("timestamp with time zone");
        });

        builder.OwnsOne(x => x.PaymentMeans, period =>
        {
            period.Property(p => p.Code)
                .HasColumnName("PaymentMeans_Code")
                .IsRequired()
                .HasMaxLength(50);

            period.Property(p => p.Name)
                .HasColumnName("PaymentMeans_Name")
                .IsRequired()
                .HasMaxLength(500);
        });

        // Navigation properties
        builder.HasOne(x => x.Business)
            .WithMany(b => b.Invoices)
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Party)
            .WithMany(p => p.Invoices)
            .HasForeignKey(x => x.PartyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Optional FK to VatSchedule — null until an invoice is included in a schedule
        builder.HasOne(x => x.VatSchedule)
            .WithMany()
            .HasForeignKey(x => x.VatScheduleId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.VatScheduleId)
            .HasDatabaseName("IX_Invoices_VatScheduleId")
            .HasFilter("\"VatScheduleId\" IS NOT NULL");

        // Invoice Lines (one-to-many relationship)
        builder.HasMany(x => x.InvoiceLine)
            .WithOne(ii => ii.Invoice)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.InvoiceCode)
            .IsUnique()
            .HasDatabaseName("IX_Invoices_InvoiceId");

        builder.HasIndex(x => x.BusinessId)
            .HasDatabaseName("IX_Invoices_BusinessId");

        builder.HasIndex(x => x.PartyId)
            .HasDatabaseName("IX_Invoices_PartyId");

        builder.HasIndex(x => x.IssueDate)
            .HasDatabaseName("IX_Invoices_IssueDate");

        builder.HasIndex(x => x.InvoiceStatus)
            .HasDatabaseName("IX_Invoices_Status");

        builder.HasIndex(x => x.PaymentStatus)
            .HasDatabaseName("IX_Invoices_PaymentStatus");

        builder.HasIndex(x => x.FIRSSubmissionId)
            .HasDatabaseName("IX_Invoices_FIRSSubmissionId");

        // Composite indexes for common queries
        builder.HasIndex(x => new { x.BusinessId, x.InvoiceStatus })
            .HasDatabaseName("IX_Invoices_BusinessId_Status");

        builder.HasIndex(x => new { x.BusinessId, x.IssueDate })
            .HasDatabaseName("IX_Invoices_BusinessId_IssueDate");

        // Audit fields configuration (inherited from AuditableAggregateRoot)
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