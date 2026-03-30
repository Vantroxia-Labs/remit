using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Infrastructure.Services.EventBus;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.InfrastructureTests.Services.EventBus;

public class ResilientEventBusTests : IDisposable
{
    private readonly Mock<IBus> _massTransitBusMock;
    private readonly Mock<IApplicationDbContext> _contextMock;
    private readonly Mock<ILogger<ResilientEventBus>> _loggerMock;
    private readonly Mock<IOptions<EventBusSettings>> _optionsMock;
    private readonly Mock<DbSet<OutboxEvent>> _outboxEventsMock;
    private readonly EventBusSettings _eventBusSettings;
    private readonly ResilientEventBus _resilientEventBus;

    public ResilientEventBusTests()
    {
        _massTransitBusMock = new Mock<IBus>();
        _contextMock = new Mock<IApplicationDbContext>();
        _loggerMock = new Mock<ILogger<ResilientEventBus>>();
        _optionsMock = new Mock<IOptions<EventBusSettings>>();
        _outboxEventsMock = new Mock<DbSet<OutboxEvent>>();

        _eventBusSettings = new EventBusSettings
        {
            RetryCount = 3,
            RetryIntervalSeconds = 1
        };

        _optionsMock.Setup(x => x.Value).Returns(_eventBusSettings);
        _contextMock.Setup(x => x.OutboxEvents).Returns(_outboxEventsMock.Object);

        _resilientEventBus = new ResilientEventBus(
            _massTransitBusMock.Object,
            _contextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ResilientEventBus(
                _massTransitBusMock.Object,
                null!,
                _optionsMock.Object,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ResilientEventBus(
                _massTransitBusMock.Object,
                _contextMock.Object,
                null!,
                _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ResilientEventBus(
                _massTransitBusMock.Object,
                _contextMock.Object,
                _optionsMock.Object,
                null!));
    }

    [Fact]
    public void Constructor_WithNullMassTransitBus_ShouldNotThrow()
    {
        // Act & Assert
        var eventBus = new ResilientEventBus(
            null,
            _contextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        eventBus.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAsync_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await _resilientEventBus.Invoking(bus => bus.PublishAsync<TestDomainEvent>(null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PublishAsync_WithHealthyRabbitMq_ShouldPublishToRabbitMq()
    {
        // Arrange
        var testEvent = new TestDomainEvent { Message = "Test event" };
        _massTransitBusMock.Setup(x => x.Publish(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _resilientEventBus.PublishAsync(testEvent, TestContext.Current.CancellationToken);

        // Assert
        _massTransitBusMock.Verify(x => x.Publish(testEvent, It.IsAny<CancellationToken>()), Times.Once);
        _outboxEventsMock.Verify(x => x.Add(It.IsAny<OutboxEvent>()), Times.Never);
    }

    [Fact]
    public async Task PublishAsync_WithNullMassTransitBus_ShouldFallbackToOutbox()
    {
        // Arrange
        var eventBusWithoutRabbitMq = new ResilientEventBus(
            null,
            _contextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var testEvent = new TestDomainEvent { Message = "Test event" };

        // Act
        await eventBusWithoutRabbitMq.PublishAsync(testEvent, TestContext.Current.CancellationToken);

        // Assert
        _outboxEventsMock.Verify(x => x.Add(It.IsAny<OutboxEvent>()), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithRabbitMqException_ShouldFallbackToOutbox()
    {
        // Arrange
        var testEvent = new TestDomainEvent { Message = "Test event" };
        _massTransitBusMock.Setup(x => x.Publish(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("RabbitMQ connection failed"));

        // Act
        await _resilientEventBus.PublishAsync(testEvent, TestContext.Current.CancellationToken);

        // Assert
        _massTransitBusMock.Verify(x => x.Publish(testEvent, It.IsAny<CancellationToken>()), Times.Once);
        _outboxEventsMock.Verify(x => x.Add(It.IsAny<OutboxEvent>()), Times.Once);
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishBatchAsync_WithEmptyList_ShouldNotPublishAnything()
    {
        // Arrange
        var emptyEvents = new List<TestDomainEvent>();

        // Act
        await _resilientEventBus.PublishBatchAsync(emptyEvents, TestContext.Current.CancellationToken);

        // Assert - Nothing should be added to outbox for empty list
        // Note: PublishBatch is an extension method and cannot be mocked/verified
        _outboxEventsMock.Verify(x => x.Add(It.IsAny<OutboxEvent>()), Times.Never);
    }

    [Fact]
    public async Task PublishBatchAsync_WithHealthyRabbitMq_ShouldNotFallbackToOutbox()
    {
        // Arrange
        var events = new List<TestDomainEvent>
        {
            new() { Message = "Event 1" },
            new() { Message = "Event 2" },
            new() { Message = "Event 3" }
        };

        // Note: PublishBatch is an extension method and cannot be mocked
        // When RabbitMQ is healthy, the extension method should work without fallback

        // Act
        await _resilientEventBus.PublishBatchAsync(events, TestContext.Current.CancellationToken);

        // Assert - When successful, nothing should go to outbox
        _outboxEventsMock.Verify(x => x.Add(It.IsAny<OutboxEvent>()), Times.Never);
    }

    [Fact]
    public async Task PublishBatchAsync_WithNullBus_ShouldFallbackToOutboxForAllEvents()
    {
        // Arrange - Use a bus without RabbitMQ to simulate failure scenario
        var eventBusWithoutRabbitMq = new ResilientEventBus(
            null,
            _contextMock.Object,
            _optionsMock.Object,
            _loggerMock.Object);

        var events = new List<TestDomainEvent>
        {
            new() { Message = "Event 1" },
            new() { Message = "Event 2" }
        };

        // Act
        await eventBusWithoutRabbitMq.PublishBatchAsync(events, TestContext.Current.CancellationToken);

        // Assert - Both events should fall back to outbox
        _outboxEventsMock.Verify(x => x.Add(It.IsAny<OutboxEvent>()), Times.Exactly(2));
        _contextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task PublishAsync_WithCancellationToken_ShouldPassCancellationToken()
    {
        // Arrange
        var testEvent = new TestDomainEvent { Message = "Test event" };
        var cancellationToken = new CancellationToken();

        _massTransitBusMock.Setup(x => x.Publish(It.IsAny<TestDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _resilientEventBus.PublishAsync(testEvent, cancellationToken);

        // Assert
        _massTransitBusMock.Verify(x => x.Publish(testEvent, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task PublishBatchAsync_WithCancellationToken_ShouldCompleteSuccessfully()
    {
        // Arrange
        var events = new List<TestDomainEvent>
        {
            new() { Message = "Event 1" }
        };
        var cancellationToken = new CancellationToken();

        // Note: PublishBatch is an extension method and cannot be mocked
        // We can only verify the method completes successfully

        // Act
        await _resilientEventBus.PublishBatchAsync(events, cancellationToken);

        // Assert - Method completes without exception and doesn't fall back to outbox
        _outboxEventsMock.Verify(x => x.Add(It.IsAny<OutboxEvent>()), Times.Never);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    // Test domain event class
    private class TestDomainEvent : IDomainEvent
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public DateTime OccurredOn { get; set; } = DateTime.UtcNow;
        public int EventVersion { get; set; } = 1;
        public string Message { get; set; } = string.Empty;
    }
}