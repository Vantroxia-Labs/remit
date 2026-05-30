using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations.InvoiceManagement;

/// <summary>
/// Entity Framework configuration for ReceivedInvoice entity
/// Configures table structure, relationships, indexes, and constraints for received invoices
/// </summary>
public sealed class ReceivedInvoiceConfiguration : IEntityTypeConfiguration<ReceivedInvoice>
{
    public void Configure(EntityTypeBuilder<ReceivedInvoice> builder)
    {
        // Table configuration
        builder.ToTable("ReceivedInvoices");

        // Primary key
        builder.HasKey(x => x.Id);

        // Core Invoice Identification
        builder.OwnsOne(x => x.Irn, irn =>
        {
            irn.Property(p => p.Value)
                .HasColumnName("IRN")
                .IsRequired()
                .HasMaxLength(100);
        });

        builder.Property(x => x.InvoiceTypeCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.IssueDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(x => x.IssueTime)
            .HasMaxLength(50);

        builder.Property(x => x.DueDate)
            .HasColumnType("date");

        // Currency Information
        builder.Property(x => x.DocumentCurrencyCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(x => x.TaxCurrencyCode)
            .IsRequired()
            .HasMaxLength(10);

        // Status Information
        builder.Property(x => x.PaymentStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.EntryStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.SyncDate)
            .HasMaxLength(100);

        // Supplier Information (Value Objects)
        builder.Property(x => x.SupplierPartyName)
            .IsRequired()
            .HasMaxLength(500);

        builder.OwnsOne(x => x.SupplierTIN, tin =>
        {
            tin.Property(p => p.Value)
                .HasColumnName("SupplierTIN")
                .IsRequired()
                .HasMaxLength(20);
        });

        builder.Property(x => x.SupplierBRN)
            .HasMaxLength(100);

        builder.Property(x => x.SupplierEmail)
            .HasMaxLength(256);

        builder.Property(x => x.SupplierTelephone)
            .HasMaxLength(50);

        builder.OwnsOne(x => x.SupplierAddress, address =>
        {
            address.Property(p => p.Street)
                .HasColumnName("SupplierAddress_Street")
                .HasMaxLength(500);

            address.Property(p => p.City)
                .HasColumnName("SupplierAddress_City")
                .HasMaxLength(200);

            address.Property(p => p.State)
                .HasColumnName("SupplierAddress_State")
                .HasMaxLength(200);

            address.Property(p => p.Country)
                .HasColumnName("SupplierAddress_Country")
                .HasMaxLength(100);

            address.Property(p => p.PostalCode)
                .HasColumnName("SupplierAddress_PostalCode")
                .HasMaxLength(50);
        });

        // Customer Information (Value Objects)
        builder.Property(x => x.CustomerPartyName)
            .IsRequired()
            .HasMaxLength(500);

        builder.OwnsOne(x => x.CustomerTIN, tin =>
        {
            tin.Property(p => p.Value)
                .HasColumnName("CustomerTIN")
                .IsRequired()
                .HasMaxLength(20);
        });

        builder.Property(x => x.CustomerBRN)
            .HasMaxLength(100);

        builder.Property(x => x.CustomerEmail)
            .HasMaxLength(256);

        builder.Property(x => x.CustomerTelephone)
            .HasMaxLength(50);

        builder.OwnsOne(x => x.CustomerAddress, address =>
        {
            address.Property(p => p.Street)
                .HasColumnName("CustomerAddress_Street")
                .HasMaxLength(500);

            address.Property(p => p.City)
                .HasColumnName("CustomerAddress_City")
                .HasMaxLength(200);

            address.Property(p => p.State)
                .HasColumnName("CustomerAddress_State")
                .HasMaxLength(200);

            address.Property(p => p.Country)
                .HasColumnName("CustomerAddress_Country")
                .HasMaxLength(100);

            address.Property(p => p.PostalCode)
                .HasColumnName("CustomerAddress_PostalCode")
                .HasMaxLength(50);
        });

        // Financial Amounts
        builder.Property(x => x.LineExtensionAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.TaxExclusiveAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.TaxInclusiveAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.TotalTaxAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.PayableAmount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.PaidAmount)
            .HasPrecision(18, 2);

        builder.Property(x => x.PayableRoundingAmount)
            .HasPrecision(18, 2);

        // Additional Information
        builder.Property(x => x.Note)
            .HasMaxLength(2000);

        builder.Property(x => x.BuyerReference)
            .HasMaxLength(200);

        builder.Property(x => x.PaymentReference)
            .HasMaxLength(200);

        builder.Property(x => x.AccountingCost)
            .HasMaxLength(200);

        // JSON fields for detailed invoice data
        builder.Property(x => x.InvoiceLinesJson)
            .HasColumnType("text");

        builder.Property(x => x.TaxTotalJson)
            .HasColumnType("text");

        builder.Property(x => x.FirsBusinessId);

        // Navigation Properties
        builder.Property(x => x.BusinessId);

        builder.HasOne(x => x.Business)
            .WithMany()
            .HasForeignKey(x => x.BusinessId)
            .OnDelete(DeleteBehavior.SetNull);

        // Reconciliation Fields
        builder.Property(x => x.IsReconciled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(x => x.ReconciledAt);

        builder.Property(x => x.ReconciledBy);

        // Audit fields (inherited from AuditableEntity)
        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        builder.Property(x => x.UpdatedBy);

        builder.Property(x => x.DeletedAt);

        builder.Property(x => x.DeletedBy);

        builder.Property(x => x.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for performance
        // Index on BusinessId for filtering invoices by business
        builder.HasIndex(x => x.BusinessId)
            .HasDatabaseName("IX_ReceivedInvoices_BusinessId");

        // Index on IssueDate for date range queries
        builder.HasIndex(x => x.IssueDate)
            .HasDatabaseName("IX_ReceivedInvoices_IssueDate");

        // Index on PaymentStatus for filtering
        builder.HasIndex(x => x.PaymentStatus)
            .HasDatabaseName("IX_ReceivedInvoices_PaymentStatus");

        // Index on IsReconciled for filtering reconciled invoices
        builder.HasIndex(x => x.IsReconciled)
            .HasDatabaseName("IX_ReceivedInvoices_IsReconciled");

        // Composite index for business and date range queries
        builder.HasIndex(x => new { x.BusinessId, x.IssueDate })
            .HasDatabaseName("IX_ReceivedInvoices_Business_IssueDate");

        builder.Property(x => x.InputVatScheduleId);

        builder.HasIndex(x => x.InputVatScheduleId)
            .HasDatabaseName("IX_ReceivedInvoices_InputVatScheduleId");

        builder.Property(x => x.WhtScheduleId);

        builder.HasIndex(x => x.WhtScheduleId)
            .HasDatabaseName("IX_ReceivedInvoices_WhtScheduleId");

        // Soft delete filter
        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}
