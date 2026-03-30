using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests.Configurations;

public class OutboxEventConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTime> _dateTimeMock;

    public OutboxEventConfigurationTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dateTimeMock = new Mock<IDateTime>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeMock.Object);
    }

    [Fact]
    public void Configure_ShouldSetPrimaryKey()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(OutboxEvent));

        // Act & Assert
        entityType.Should().NotBeNull();
        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");
    }

    [Fact]
    public void Configure_ShouldConfigureTableName()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(OutboxEvent));

        // Act & Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("OutboxEvents");
    }

    [Fact]
    public void Configure_ShouldConfigureRequiredProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(OutboxEvent));

        // Act & Assert
        entityType.Should().NotBeNull();

        // Check EventType property
        var eventTypeProperty = entityType!.FindProperty("EventType");
        eventTypeProperty.Should().NotBeNull();
        eventTypeProperty!.IsNullable.Should().BeFalse();
        eventTypeProperty.GetMaxLength().Should().Be(500);

        // Check EventData property
        var eventDataProperty = entityType.FindProperty("EventData");
        eventDataProperty.Should().NotBeNull();
        eventDataProperty!.IsNullable.Should().BeFalse();

        // Check Status property
        var statusProperty = entityType.FindProperty("Status");
        statusProperty.Should().NotBeNull();
        statusProperty!.IsNullable.Should().BeFalse();

        // Check OccurredOnUtc property
        var occurredOnUtcProperty = entityType.FindProperty("OccurredOnUtc");
        occurredOnUtcProperty.Should().NotBeNull();
        occurredOnUtcProperty!.IsNullable.Should().BeFalse();

        // Check CreatedAt property
        var createdAtProperty = entityType.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldConfigureOptionalProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(OutboxEvent));

        // Act & Assert
        entityType.Should().NotBeNull();

        // Check ProcessedOnUtc property
        var processedOnUtcProperty = entityType!.FindProperty("ProcessedOnUtc");
        processedOnUtcProperty.Should().NotBeNull();
        processedOnUtcProperty!.IsNullable.Should().BeTrue();

        // Check Error property
        var errorProperty = entityType.FindProperty("Error");
        errorProperty.Should().NotBeNull();
        errorProperty!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldConfigureStatusAsEnum()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(OutboxEvent));

        // Act & Assert
        entityType.Should().NotBeNull();

        var statusProperty = entityType!.FindProperty("Status");
        statusProperty.Should().NotBeNull();

        // Verify Status property is configured with correct max length (from HasConversion<string>())
        // InMemory database may not report the actual ValueConverter but the configuration is applied
        statusProperty!.GetMaxLength().Should().Be(50);
        statusProperty.ClrType.Should().Be(typeof(OutboxEventStatus));
    }

    [Fact]
    public void Configure_ShouldConfigureIndexes()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(OutboxEvent));

        // Act & Assert
        entityType.Should().NotBeNull();

        var indexes = entityType!.GetIndexes();
        indexes.Should().NotBeEmpty();

        // Check for Status index
        var statusIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "Status"));
        statusIndex.Should().NotBeNull();

        // Check for CreatedAt index
        var createdAtIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "CreatedAt"));
        createdAtIndex.Should().NotBeNull();

        // Check for OccurredOnUtc index
        var occurredOnUtcIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "OccurredOnUtc"));
        occurredOnUtcIndex.Should().NotBeNull();
    }

    [Fact]
    public async Task Configuration_ShouldWorkWithActualEntity()
    {
        // Arrange
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.CreateVersion7(),
            EventType = "TestEvent",
            EventData = "{\"message\": \"test data\"}",
            Status = OutboxEventStatus.Pending,
            OccurredOnUtc = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedEvent = await _context.OutboxEvents.FirstOrDefaultAsync(e => e.Id == outboxEvent.Id, TestContext.Current.CancellationToken);
        savedEvent.Should().NotBeNull();
        savedEvent!.EventType.Should().Be("TestEvent");
        savedEvent.EventData.Should().Be("{\"message\": \"test data\"}");
        savedEvent.Status.Should().Be(OutboxEventStatus.Pending);
        savedEvent.ProcessedOnUtc.Should().BeNull();
        savedEvent.Error.Should().BeNull();
    }

    [Fact]
    public async Task Configuration_ShouldSupportAllOutboxEventStatuses()
    {
        // Arrange & Act
        var events = new[]
        {
            new OutboxEvent
            {
                Id = Guid.CreateVersion7(),
                EventType = "PendingEvent",
                EventData = "{}",
                Status = OutboxEventStatus.Pending,
                OccurredOnUtc = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OutboxEvent
            {
                Id = Guid.CreateVersion7(),
                EventType = "ProcessingEvent",
                EventData = "{}",
                Status = OutboxEventStatus.Processing,
                OccurredOnUtc = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OutboxEvent
            {
                Id = Guid.CreateVersion7(),
                EventType = "CompletedEvent",
                EventData = "{}",
                Status = OutboxEventStatus.Processed,
                OccurredOnUtc = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                ProcessedOnUtc = DateTimeOffset.UtcNow
            },
            new OutboxEvent
            {
                Id = Guid.CreateVersion7(),
                EventType = "FailedEvent",
                EventData = "{}",
                Status = OutboxEventStatus.Failed,
                OccurredOnUtc = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow,
                ProcessedOnUtc = DateTimeOffset.UtcNow,
                Error = "Test error message"
            }
        };

        _context.OutboxEvents.AddRange(events);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedEvents = await _context.OutboxEvents.ToListAsync(TestContext.Current.CancellationToken);
        savedEvents.Should().HaveCount(4);

        var pendingEvent = savedEvents.First(e => e.Status == OutboxEventStatus.Pending);
        pendingEvent.Should().NotBeNull();

        var processingEvent = savedEvents.First(e => e.Status == OutboxEventStatus.Processing);
        processingEvent.Should().NotBeNull();

        var completedEvent = savedEvents.First(e => e.Status == OutboxEventStatus.Processed);
        completedEvent.Should().NotBeNull();
        completedEvent.ProcessedOnUtc.Should().NotBeNull();

        var failedEvent = savedEvents.First(e => e.Status == OutboxEventStatus.Failed);
        failedEvent.Should().NotBeNull();
        failedEvent.Error.Should().Be("Test error message");
    }

    [Fact]
    public void Configure_ShouldHaveCorrectColumnTypes()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(OutboxEvent));

        // Act & Assert
        entityType.Should().NotBeNull();

        var createdAtProperty = entityType!.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();

        var occurredOnUtcProperty = entityType.FindProperty("OccurredOnUtc");
        occurredOnUtcProperty.Should().NotBeNull();

        var processedOnUtcProperty = entityType.FindProperty("ProcessedOnUtc");
        processedOnUtcProperty.Should().NotBeNull();
    }

    [Fact]
    public async Task Configuration_ShouldSupportLargeEventData()
    {
        // Arrange
        var largeEventData = new string('x', 10000); // 10KB of data
        var outboxEvent = new OutboxEvent
        {
            Id = Guid.CreateVersion7(),
            EventType = "LargeEvent",
            EventData = largeEventData,
            Status = OutboxEventStatus.Pending,
            OccurredOnUtc = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Act
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedEvent = await _context.OutboxEvents.FirstOrDefaultAsync(e => e.Id == outboxEvent.Id, TestContext.Current.CancellationToken);
        savedEvent.Should().NotBeNull();
        savedEvent!.EventData.Should().Be(largeEventData);
        savedEvent.EventData.Length.Should().Be(10000);
    }

    [Fact]
    public async Task Configuration_ShouldSupportQueryingByStatus()
    {
        // Arrange
        var events = new[]
        {
            new OutboxEvent
            {
                Id = Guid.CreateVersion7(),
                EventType = "Event1",
                EventData = "{}",
                Status = OutboxEventStatus.Pending,
                OccurredOnUtc = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new OutboxEvent
            {
                Id = Guid.CreateVersion7(),
                EventType = "Event2",
                EventData = "{}",
                Status = OutboxEventStatus.Processed,
                OccurredOnUtc = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _context.OutboxEvents.AddRange(events);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var pendingEvents = await _context.OutboxEvents
            .Where(e => e.Status == OutboxEventStatus.Pending)
            .ToListAsync(TestContext.Current.CancellationToken);

        // Assert
        pendingEvents.Should().HaveCount(1);
        pendingEvents[0].EventType.Should().Be("Event1");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}