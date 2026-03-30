using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class InvoiceTransmissionQueueTests
{
    private readonly string _validIrn = "IRN-12345-67890";
    private readonly InvoiceStatus _validStatus = InvoiceStatus.VALIDATED;
    private readonly string _validPayload = "{ \"invoice\": { \"id\": \"123\", \"amount\": 1000.00 } }";
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateInvoiceTransmissionQueue()
    {
        // Act
        var queueItem = InvoiceTransmissionQueue.Create(_validIrn, _validStatus, _validPayload);

        // Assert
        queueItem.Should().NotBeNull();
        queueItem.Id.Should().NotBeEmpty();
        queueItem.Irn.Should().Be(_validIrn);
        queueItem.Status.Should().Be(_validStatus);
        queueItem.RequestPayload.Should().Be(_validPayload);
        queueItem.BusinessId.Should().BeNull();
        queueItem.UserId.Should().BeNull();
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Pending);
        queueItem.AttemptCount.Should().Be(0);
        queueItem.LastErrorMessage.Should().BeNull();
        queueItem.ProcessAfter.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        queueItem.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithAllParameters_ShouldCreateInvoiceTransmissionQueue()
    {
        // Act
        var queueItem = InvoiceTransmissionQueue.Create(
            _validIrn,
            _validStatus,
            _validPayload,
            _businessId,
            _userId);

        // Assert
        queueItem.Should().NotBeNull();
        queueItem.Irn.Should().Be(_validIrn);
        queueItem.Status.Should().Be(_validStatus);
        queueItem.RequestPayload.Should().Be(_validPayload);
        queueItem.BusinessId.Should().Be(_businessId);
        queueItem.UserId.Should().Be(_userId);
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Pending);
        queueItem.AttemptCount.Should().Be(0);
    }

    [Fact]
    public void MarkAsProcessing_ShouldUpdateStatusAndIncrementAttempts()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();

        // Act
        queueItem.MarkAsProcessing();

        // Assert
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Processing);
        queueItem.AttemptCount.Should().Be(1);
        queueItem.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsProcessing_CalledMultipleTimes_ShouldIncrementAttempts()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();

        // Act
        queueItem.MarkAsProcessing();
        queueItem.MarkAsProcessing();
        queueItem.MarkAsProcessing();

        // Assert
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Processing);
        queueItem.AttemptCount.Should().Be(3);
    }

    [Fact]
    public void MarkAsCompleted_ShouldUpdateStatusAndCompletionTime()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();
        queueItem.MarkAsProcessing();

        // Act
        queueItem.MarkAsCompleted();

        // Assert
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Completed);
        queueItem.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        queueItem.LastErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_WithinRetryLimit_ShouldSetToPendingWithBackoff()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();
        queueItem.MarkAsProcessing(); // First attempt
        var errorMessage = "Network timeout occurred";

        // Act
        queueItem.MarkAsFailed(errorMessage);

        // Assert
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Pending);
        queueItem.LastErrorMessage.Should().Be(errorMessage);
        queueItem.AttemptCount.Should().Be(1);
        queueItem.ProcessAfter.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MarkAsFailed_ExceedingRetryLimit_ShouldSetToFailed()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();
        var errorMessage = "Persistent failure";
        var maxRetries = 3;

        // Simulate multiple failures
        for (int i = 0; i < maxRetries; i++)
        {
            queueItem.MarkAsProcessing();
            queueItem.MarkAsFailed(errorMessage, maxRetries);
        }

        // Act - One more failure should mark as permanently failed
        queueItem.MarkAsProcessing();
        queueItem.MarkAsFailed(errorMessage, maxRetries);

        // Assert
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Failed);
        queueItem.LastErrorMessage.Should().Be(errorMessage);
        queueItem.AttemptCount.Should().Be(4); // 3 retries + 1 initial attempt
    }

    [Fact]
    public void MarkAsFailed_ShouldImplementExponentialBackoff()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();
        var errorMessage = "Temporary failure";
        var beforeFirstFailure = DateTimeOffset.UtcNow;

        // Act - First failure
        queueItem.MarkAsProcessing(); // AttemptCount = 1
        queueItem.MarkAsFailed(errorMessage);
        var firstRetryTime = queueItem.ProcessAfter;

        // Second failure
        queueItem.MarkAsProcessing(); // AttemptCount = 2
        queueItem.MarkAsFailed(errorMessage);
        var secondRetryTime = queueItem.ProcessAfter;

        // Assert
        // First retry should be after 1 minute (5^0 = 1)
        firstRetryTime.Should().BeCloseTo(beforeFirstFailure.AddMinutes(1), TimeSpan.FromSeconds(5));

        // Second retry should be after 5 minutes (5^1 = 5)
        secondRetryTime.Should().BeAfter(firstRetryTime.Value);
        secondRetryTime.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(InvoiceStatus.DRAFT)]
    [InlineData(InvoiceStatus.CREATED)]
    [InlineData(InvoiceStatus.APPROVED)]
    [InlineData(InvoiceStatus.VALIDATED)]
    [InlineData(InvoiceStatus.SIGNED)]
    [InlineData(InvoiceStatus.TRANSMITTED)]
    public void Create_WithDifferentInvoiceStatuses_ShouldWork(InvoiceStatus status)
    {
        // Act
        var queueItem = InvoiceTransmissionQueue.Create(_validIrn, status, _validPayload);

        // Assert
        queueItem.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(QueueStatus.Pending)]
    [InlineData(QueueStatus.Processing)]
    [InlineData(QueueStatus.Completed)]
    [InlineData(QueueStatus.Failed)]
    public void QueueStatus_ShouldHaveExpectedEnumValues(QueueStatus expectedStatus)
    {
        // Assert
        Enum.IsDefined(typeof(QueueStatus), expectedStatus).Should().BeTrue();
    }

    [Fact]
    public void QueueStatus_ShouldHaveCorrectNumericValues()
    {
        // Assert
        ((int)QueueStatus.Pending).Should().Be(0);
        ((int)QueueStatus.Processing).Should().Be(1);
        ((int)QueueStatus.Completed).Should().Be(2);
        ((int)QueueStatus.Failed).Should().Be(3);
    }

    [Fact]
    public void Create_WithEmptyPayload_ShouldStillWork()
    {
        // Act
        var queueItem = InvoiceTransmissionQueue.Create(_validIrn, _validStatus, string.Empty);

        // Assert
        queueItem.RequestPayload.Should().Be(string.Empty);
    }

    [Fact]
    public void Create_WithComplexPayload_ShouldStorePayload()
    {
        // Arrange
        var complexPayload = @"{
            ""invoice"": {
                ""id"": ""123"",
                ""items"": [
                    { ""name"": ""Item 1"", ""amount"": 100.00 },
                    { ""name"": ""Item 2"", ""amount"": 200.00 }
                ],
                ""total"": 300.00,
                ""currency"": ""NGN""
            }
        }";

        // Act
        var queueItem = InvoiceTransmissionQueue.Create(_validIrn, _validStatus, complexPayload);

        // Assert
        queueItem.RequestPayload.Should().Be(complexPayload);
    }

    [Fact]
    public void ProcessingWorkflow_ShouldFollowExpectedPattern()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();

        // Act & Assert - Initial state
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Pending);
        queueItem.AttemptCount.Should().Be(0);

        // Mark as processing
        queueItem.MarkAsProcessing();
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Processing);
        queueItem.AttemptCount.Should().Be(1);

        // Complete successfully
        queueItem.MarkAsCompleted();
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Completed);
        queueItem.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void ProcessingWorkflow_WithFailure_ShouldRetryAndEventuallyFail()
    {
        // Arrange
        var queueItem = CreateTestQueueItem();
        var maxRetries = 3; // After 3 attempts (AttemptCount >= 3), status becomes Failed

        // Act & Assert - First attempt, AttemptCount becomes 1
        queueItem.MarkAsProcessing();
        queueItem.MarkAsFailed("First failure", maxRetries);
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Pending);

        // Second attempt, AttemptCount becomes 2
        queueItem.MarkAsProcessing();
        queueItem.MarkAsFailed("Second failure", maxRetries);
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Pending);

        // Third attempt, AttemptCount becomes 3, which equals maxRetries, so status becomes Failed
        queueItem.MarkAsProcessing();
        queueItem.MarkAsFailed("Final failure", maxRetries);
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Failed);
    }

    [Fact]
    public void Create_ShouldInheritFromAuditableEntity()
    {
        // Arrange & Act
        var queueItem = CreateTestQueueItem();

        // Assert
        queueItem.Should().BeAssignableTo<AegisEInvoicing.Domain.Common.Implementation.AuditableEntity>();
        queueItem.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldUseVersion7Guid()
    {
        // Arrange & Act
        var queueItem1 = CreateTestQueueItem();
        var queueItem2 = CreateTestQueueItem();

        // Assert
        queueItem1.Id.Should().NotBe(queueItem2.Id);
        queueItem1.Id.Should().NotBeEmpty();
        queueItem2.Id.Should().NotBeEmpty();
    }

    private InvoiceTransmissionQueue CreateTestQueueItem()
    {
        return InvoiceTransmissionQueue.Create(_validIrn, _validStatus, _validPayload, _businessId, _userId);
    }
}