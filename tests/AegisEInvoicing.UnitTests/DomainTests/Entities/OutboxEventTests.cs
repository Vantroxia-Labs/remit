using AegisEInvoicing.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class OutboxEventTests
{
    [Fact]
    public void Create_WithValidParameters_ShouldCreateOutboxEvent()
    {
        // Arrange
        var eventType = "InvoiceCreated";
        var eventData = "{ \"invoiceId\": \"123\" }";

        // Act
        var outboxEvent = OutboxEvent.Create(eventType, eventData);

        // Assert
        outboxEvent.Should().NotBeNull();
        outboxEvent.Id.Should().NotBeEmpty();
        outboxEvent.EventType.Should().Be(eventType);
        outboxEvent.EventData.Should().Be(eventData);
        outboxEvent.OccurredOnUtc.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        outboxEvent.CreatedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        outboxEvent.ProcessedOnUtc.Should().BeNull();
        outboxEvent.Error.Should().BeNull();
        outboxEvent.RetryCount.Should().Be(0);
        outboxEvent.Status.Should().Be(OutboxEventStatus.Pending);
        outboxEvent.IsProcessed.Should().BeFalse();
    }

    [Fact]
    public void MarkAsProcessed_ShouldUpdateStatusAndProcessedTime()
    {
        // Arrange
        var outboxEvent = OutboxEvent.Create("TestEvent", "{}");

        // Act
        outboxEvent.MarkAsProcessed();

        // Assert
        outboxEvent.Status.Should().Be(OutboxEventStatus.Processed);
        outboxEvent.ProcessedOnUtc.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
        outboxEvent.IsProcessed.Should().BeTrue();
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndError()
    {
        // Arrange
        var outboxEvent = OutboxEvent.Create("TestEvent", "{}");
        var errorMessage = "Processing failed due to network error";

        // Act
        outboxEvent.MarkAsFailed(errorMessage);

        // Assert
        outboxEvent.Status.Should().Be(OutboxEventStatus.Failed);
        outboxEvent.Error.Should().Be(errorMessage);
        outboxEvent.RetryCount.Should().Be(1);
    }

    [Fact]
    public void MarkAsFailed_CalledMultipleTimes_ShouldIncrementRetryCount()
    {
        // Arrange
        var outboxEvent = OutboxEvent.Create("TestEvent", "{}");

        // Act
        outboxEvent.MarkAsFailed("First failure");
        outboxEvent.MarkAsFailed("Second failure");
        outboxEvent.MarkAsFailed("Third failure");

        // Assert
        outboxEvent.Status.Should().Be(OutboxEventStatus.Failed);
        outboxEvent.Error.Should().Be("Third failure");
        outboxEvent.RetryCount.Should().Be(3);
    }

    [Fact]
    public void MarkAsProcessing_ShouldUpdateStatus()
    {
        // Arrange
        var outboxEvent = OutboxEvent.Create("TestEvent", "{}");

        // Act
        outboxEvent.MarkAsProcessing();

        // Assert
        outboxEvent.Status.Should().Be(OutboxEventStatus.Processing);
        outboxEvent.ProcessedOnUtc.Should().BeNull();
        outboxEvent.IsProcessed.Should().BeFalse();
    }

    [Fact]
    public void IsProcessed_WithProcessedOnUtc_ShouldReturnTrue()
    {
        // Arrange
        var outboxEvent = OutboxEvent.Create("TestEvent", "{}");
        outboxEvent.MarkAsProcessed();

        // Act & Assert
        outboxEvent.IsProcessed.Should().BeTrue();
    }

    [Fact]
    public void IsProcessed_WithoutProcessedOnUtc_ShouldReturnFalse()
    {
        // Arrange
        var outboxEvent = OutboxEvent.Create("TestEvent", "{}");

        // Act & Assert
        outboxEvent.IsProcessed.Should().BeFalse();
    }

    [Theory]
    [InlineData(OutboxEventStatus.Pending)]
    [InlineData(OutboxEventStatus.Processing)]
    [InlineData(OutboxEventStatus.Processed)]
    [InlineData(OutboxEventStatus.Failed)]
    public void Status_ShouldBeSettableToAllValidValues(OutboxEventStatus status)
    {
        // Arrange
        var outboxEvent = OutboxEvent.Create("TestEvent", "{}");

        // Act
        outboxEvent.Status = status;

        // Assert
        outboxEvent.Status.Should().Be(status);
    }
}