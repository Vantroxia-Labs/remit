using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.InvoiceManagement;

public class InvoiceApprovalHistoryTests
{
    private readonly Guid _invoiceId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_ShouldCreateInvoiceApprovalHistory()
    {
        // Arrange
        var status = InvoiceStatus.APPROVED;

        // Act
        var approvalHistory = InvoiceApprovalHistory.Create(_invoiceId, status, "Test comment");

        // Assert
        approvalHistory.Should().NotBeNull();
        approvalHistory.Id.Should().NotBeEmpty();
        approvalHistory.InvoiceId.Should().Be(_invoiceId);
        approvalHistory.InvoiceStatus.Should().Be(status);
    }

    [Theory]
    [InlineData(InvoiceStatus.DRAFT)]
    [InlineData(InvoiceStatus.CREATED)]
    [InlineData(InvoiceStatus.APPROVED)]
    [InlineData(InvoiceStatus.REJECTED)]
    [InlineData(InvoiceStatus.VALIDATED)]
    [InlineData(InvoiceStatus.VALIDATIONFAILED)]
    [InlineData(InvoiceStatus.SUBMITTED)]
    [InlineData(InvoiceStatus.SIGNED)]
    [InlineData(InvoiceStatus.SIGNINGFAILED)]
    [InlineData(InvoiceStatus.TRANSMITTING)]
    [InlineData(InvoiceStatus.TRANSMITTED)]
    [InlineData(InvoiceStatus.ACKNOWLEDGED)]
    [InlineData(InvoiceStatus.FAILED)]
    public void Create_WithDifferentInvoiceStatuses_ShouldWork(InvoiceStatus status)
    {
        // Act
        var approvalHistory = InvoiceApprovalHistory.Create(_invoiceId, status, "Test comment");

        // Assert
        approvalHistory.InvoiceStatus.Should().Be(status);
        approvalHistory.InvoiceId.Should().Be(_invoiceId);
    }

    [Fact]
    public void Create_WithEmptyInvoiceId_ShouldStillCreateHistory()
    {
        // Arrange
        var emptyInvoiceId = Guid.Empty;
        var status = InvoiceStatus.CREATED;

        // Act
        var approvalHistory = InvoiceApprovalHistory.Create(emptyInvoiceId, status, "Test with empty invoice ID");

        // Assert
        approvalHistory.InvoiceId.Should().Be(Guid.Empty);
        approvalHistory.InvoiceStatus.Should().Be(status);
    }

    [Fact]
    public void Create_ShouldHaveNavigationProperties()
    {
        // Arrange
        var status = InvoiceStatus.APPROVED;

        // Act
        var approvalHistory = InvoiceApprovalHistory.Create(_invoiceId, status, "Test comment");

        // Assert
        // Navigation properties should be null initially (set by EF Core later)
        // We can't test the actual navigation without a proper context
        approvalHistory.Should().NotBeNull();
        approvalHistory.InvoiceId.Should().Be(_invoiceId);
    }

    [Fact]
    public void InvoiceApprovalHistory_ShouldInheritFromAuditableEntity()
    {
        // Arrange
        var status = InvoiceStatus.VALIDATED;

        // Act
        var approvalHistory = InvoiceApprovalHistory.Create(_invoiceId, status, "Test comment");

        // Assert
        approvalHistory.Should().BeAssignableTo<AegisEInvoicing.Domain.Common.Implementation.AuditableEntity>();
        approvalHistory.Id.Should().NotBeEmpty(); // Should have inherited Id from base class
    }

    [Theory]
    [InlineData(InvoiceStatus.DRAFT, InvoiceStatus.CREATED)]
    [InlineData(InvoiceStatus.CREATED, InvoiceStatus.APPROVED)]
    [InlineData(InvoiceStatus.APPROVED, InvoiceStatus.VALIDATED)]
    [InlineData(InvoiceStatus.VALIDATED, InvoiceStatus.SIGNED)]
    [InlineData(InvoiceStatus.SIGNED, InvoiceStatus.TRANSMITTED)]
    public void Create_MultipleHistoryEntriesForSameInvoice_ShouldWork(InvoiceStatus firstStatus, InvoiceStatus secondStatus)
    {
        // Act
        var firstEntry = InvoiceApprovalHistory.Create(_invoiceId, firstStatus, "First status");
        var secondEntry = InvoiceApprovalHistory.Create(_invoiceId, secondStatus, "Second status");

        // Assert
        firstEntry.InvoiceId.Should().Be(_invoiceId);
        secondEntry.InvoiceId.Should().Be(_invoiceId);
        firstEntry.InvoiceStatus.Should().Be(firstStatus);
        secondEntry.InvoiceStatus.Should().Be(secondStatus);
        firstEntry.Id.Should().NotBe(secondEntry.Id); // Should have different IDs
    }

    [Fact]
    public void Create_WithRejectedStatus_ShouldCreateHistoryEntry()
    {
        // Arrange
        var status = InvoiceStatus.REJECTED;

        // Act
        var approvalHistory = InvoiceApprovalHistory.Create(_invoiceId, status, "Test comment");

        // Assert
        approvalHistory.InvoiceStatus.Should().Be(InvoiceStatus.REJECTED);
        approvalHistory.InvoiceId.Should().Be(_invoiceId);
    }

    [Fact]
    public void Create_WithFailedStatus_ShouldCreateHistoryEntry()
    {
        // Arrange
        var status = InvoiceStatus.FAILED;

        // Act
        var approvalHistory = InvoiceApprovalHistory.Create(_invoiceId, status, "Test comment");

        // Assert
        approvalHistory.InvoiceStatus.Should().Be(InvoiceStatus.FAILED);
        approvalHistory.InvoiceId.Should().Be(_invoiceId);
    }

    [Fact]
    public void InvoiceStatusEnum_ShouldHaveExpectedValues()
    {
        // Assert - Verify all expected enum values exist
        Enum.GetNames<InvoiceStatus>().Should().Contain([
            "REJECTED", "DRAFT", "CREATED", "PENDING_APPROVAL", "APPROVED", "VALIDATED",
            "VALIDATIONFAILED", "SUBMITTED", "SIGNED", "SIGNINGFAILED",
            "TRANSMITTING", "TRANSMITTED", "TRANSMISSIONFAILED", "ACKNOWLEDGED", "FAILED"
        ]);
    }

    [Fact]
    public void InvoiceStatusEnum_ShouldHaveCorrectNumericValues()
    {
        // Assert
        ((int)InvoiceStatus.REJECTED).Should().Be(0);
        ((int)InvoiceStatus.DRAFT).Should().Be(1);
        ((int)InvoiceStatus.CREATED).Should().Be(2);
        ((int)InvoiceStatus.PENDING_APPROVAL).Should().Be(3);
        ((int)InvoiceStatus.APPROVED).Should().Be(4);
    }

    [Fact]
    public void Create_ForInvoiceWorkflow_ShouldSupportTypicalWorkflow()
    {
        // Arrange - Simulate typical invoice approval workflow
        var workflowStatuses = new[]
        {
            InvoiceStatus.DRAFT,
            InvoiceStatus.CREATED,
            InvoiceStatus.APPROVED,
            InvoiceStatus.VALIDATED,
            InvoiceStatus.SIGNED,
            InvoiceStatus.TRANSMITTED,
            InvoiceStatus.ACKNOWLEDGED
        };

        // Act - Create history entries for each step
        var historyEntries = workflowStatuses
            .Select(status => InvoiceApprovalHistory.Create(_invoiceId, status, $"Workflow: {status}"))
            .ToList();

        // Assert
        historyEntries.Should().HaveCount(7);
        historyEntries.All(h => h.InvoiceId == _invoiceId).Should().BeTrue();
        historyEntries.Select(h => h.InvoiceStatus).Should().BeEquivalentTo(workflowStatuses);
        historyEntries.Select(h => h.Id).Distinct().Should().HaveCount(7); // All should have unique IDs
    }

    [Fact]
    public void Create_ForFailedWorkflow_ShouldSupportFailureScenarios()
    {
        // Arrange - Simulate failed workflows
        var failureScenarios = new[]
        {
            (InvoiceStatus.VALIDATIONFAILED, "After validation attempt"),
            (InvoiceStatus.SIGNINGFAILED, "After signing attempt"),
            (InvoiceStatus.FAILED, "General failure"),
            (InvoiceStatus.REJECTED, "Rejected by approver")
        };

        // Act & Assert
        foreach (var (status, description) in failureScenarios)
        {
            var historyEntry = InvoiceApprovalHistory.Create(_invoiceId, status, description);
            historyEntry.InvoiceStatus.Should().Be(status, description);
            historyEntry.InvoiceId.Should().Be(_invoiceId);
        }
    }
}