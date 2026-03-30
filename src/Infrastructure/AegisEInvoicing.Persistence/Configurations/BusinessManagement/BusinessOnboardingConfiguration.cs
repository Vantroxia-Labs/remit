using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for BusinessOnboarding entity
/// Configures table structure, indexes, and constraints for business onboarding process
/// </summary>
public class BusinessOnboardingConfiguration : IEntityTypeConfiguration<BusinessOnboarding>
{
    public void Configure(EntityTypeBuilder<BusinessOnboarding> builder)
    {
        // Table configuration - Use PascalCase plural naming
        builder.ToTable("BusinessOnboardings");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id);

        builder.Property(e => e.CompanyName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.BusinessRegistrationNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.OwnsOne(e => e.TaxIdentificationNumber, tin =>
        {
            tin.Property(t => t.Value)
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.OwnsOne(e => e.RegisteredAddress, address =>
        {
            address.Property(a => a.Street)
                .HasMaxLength(200)
                .IsRequired();

            address.Property(a => a.City)
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.State)
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.Country)
                .HasMaxLength(100)
                .IsRequired();

            address.Property(a => a.PostalCode)
                .HasMaxLength(20)
                .IsRequired();
        });

        builder.Property(e => e.ContactEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.ContactPhone)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ContactPersonName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ContactPersonTitle)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DeploymentType)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.OnPremiseDetails)
            .HasColumnType("jsonb");

        builder.Property(e => e.DomainWhitelist)
            .HasColumnType("jsonb");

        builder.Property(e => e.FIRSApiKey)
            .HasMaxLength(500);

        builder.Property(e => e.FIRSApiSecret)
            .HasMaxLength(500);

        builder.Property(e => e.FIRSServiceId)
            .HasMaxLength(100);

        builder.Property(e => e.FIRSSecretKey)
            .HasMaxLength(500);

        builder.Property(e => e.HasFIRSCredentials)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.ExpectedMonthlyInvoices)
            .IsRequired();

        builder.Property(e => e.ExpectedUsers)
            .IsRequired();

        builder.Property(e => e.SpecialRequirements)
            .HasColumnType("text");

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.StatusReason)
            .HasMaxLength(500);

        builder.Property(e => e.StatusLastChanged)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.AssignedKMPGReviewer);

        builder.Property(e => e.ReviewStartedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.ReviewCompletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.ReviewNotes)
            .HasColumnType("text");

        builder.Property(e => e.RiskAssessment)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.ApprovedBy);

        builder.Property(e => e.ApprovedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.ApprovalNotes)
            .HasColumnType("text");

        builder.Property(e => e.RejectedBy);

        builder.Property(e => e.RejectedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.RejectionReason)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedBusinessId);

        builder.Property(e => e.BusinessCreatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UploadedDocuments)
            .HasColumnType("jsonb");

        builder.Property(e => e.ComplianceCheckPassed)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(e => e.ComplianceNotes)
            .HasColumnType("text");

        // Audit fields configuration (inherited from AuditableEntity)
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

        // Indexes with proper naming convention
        builder.HasIndex(e => e.CompanyName)
            .HasDatabaseName("IX_BusinessOnboardings_CompanyName");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_BusinessOnboardings_Status");

        builder.HasIndex(e => e.DeploymentType)
            .HasDatabaseName("IX_BusinessOnboardings_DeploymentType");

        builder.HasIndex(e => e.AssignedKMPGReviewer)
            .HasDatabaseName("IX_BusinessOnboardings_AssignedReviewer");

        builder.HasIndex(e => e.CreatedBusinessId)
            .HasDatabaseName("IX_BusinessOnboardings_CreatedBusinessId");

        builder.HasIndex(e => e.IsDeleted)
            .HasDatabaseName("IX_BusinessOnboardings_IsDeleted");

        // Composite indexes for common queries
        builder.HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_BusinessOnboardings_Status_CreatedAt");
        builder.HasIndex(e => new { e.AssignedKMPGReviewer, e.Status })
            .HasDatabaseName("IX_BusinessOnboardings_AssignedReviewer_Status");
        builder.HasIndex(e => new { e.IsDeleted, e.Status })
            .HasDatabaseName("IX_BusinessOnboardings_IsDeleted_Status");

        // Add soft delete query filter
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);
    }
}