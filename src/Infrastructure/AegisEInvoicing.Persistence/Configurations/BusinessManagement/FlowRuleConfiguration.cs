using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AegisEInvoicing.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ApprovalWorkflow entity
/// </summary>
public class FlowRuleConfiguration : IEntityTypeConfiguration<FlowRule>
{
    public void Configure(EntityTypeBuilder<FlowRule> builder)
    {
        builder.HasKey(e => e.Id);

        // Configure Guid properties
        builder.Property(e => e.Id);

        builder.Property(e => e.BusinessId)
            .IsRequired();

        builder.Property(e => e.Description)
            .IsRequired();

        builder.Property(e => e.Name);

        // Legacy Amount field - kept for backward compatibility
#pragma warning disable CS0618 // Type or member is obsolete
        builder.Property(e => e.Amount)
            .IsRequired();
#pragma warning restore CS0618 // Type or member is obsolete

        // Range-based amount fields
        builder.Property(e => e.MinAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.MaxAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired()
            .HasDefaultValue(9999999999999999.99m);  // Max value for decimal(18,2)

        builder.Property(e => e.RequiresClientAdminApproval)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.Priority)
            .IsRequired()
            .HasDefaultValue(1);

        // Time-based rule fields
        builder.Property(e => e.EnableTimeBasedRules)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ActiveStartTime)
            .HasColumnType("interval");

        builder.Property(e => e.ActiveEndTime)
            .HasColumnType("interval");

        builder.Property(e => e.ActiveDaysOfWeek)
            .HasConversion(
                v => v != null && v.Any() ? string.Join(',', v.Select(x => (int)x)) : null,
                v => !string.IsNullOrEmpty(v)
                    ? v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                       .Select(x => (DayOfWeek)int.Parse(x))
                       .ToArray()
                    : null)
            .HasColumnName("active_days_of_week");

        // Configure auditable properties (inherited from AuditableAggregateRoot)
        builder.Property(e => e.CreatedBy);

        builder.Property(e => e.UpdatedBy);

        builder.Property(e => e.DeletedBy);
       

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.UpdatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.DeletedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(e => e.IsDeleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Relationships - Many FlowRules can belong to one Business
        builder.HasOne(fr => fr.Business)
           .WithMany(b => b.FlowRules)
           .HasForeignKey(fr => fr.BusinessId)
           .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(i => i.BusinessId);
        builder.HasIndex(i => i.CreatedBy);
        builder.HasIndex(i => i.CreatedAt);
        builder.HasIndex(e => e.IsDeleted);

        // Composite index for efficient duplicate name checks within a business
        builder.HasIndex(e => new { e.BusinessId, e.Name, e.IsDeleted })
            .HasDatabaseName("IX_flow_rules_BusinessId_Name_IsDeleted");

        // Composite index for efficient range queries and matching
        builder.HasIndex(e => new { e.BusinessId, e.MinAmount, e.MaxAmount, e.Priority, e.IsDeleted })
            .HasDatabaseName("IX_flow_rules_BusinessId_Range_Priority");

        // Add soft delete query filter
        builder.HasQueryFilter(e => !e.IsDeleted);

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);

        // Table configuration - Use PascalCase plural naming
        builder.ToTable("FlowRules");
    }
}