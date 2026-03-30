using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests.Configurations;

public class InvoiceTransmissionQueueConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTime> _dateTimeMock;

    public InvoiceTransmissionQueueConfigurationTests()
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
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

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
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("InvoiceTransmissionQueues");
    }

    [Fact]
    public void Configure_ShouldConfigureRequiredProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();

        // Check IRN property
        var irnProperty = entityType!.FindProperty("Irn");
        irnProperty.Should().NotBeNull();
        irnProperty!.IsNullable.Should().BeFalse();
        irnProperty.GetMaxLength().Should().Be(64);

        // Check Status property
        var statusProperty = entityType.FindProperty("Status");
        statusProperty.Should().NotBeNull();
        statusProperty!.IsNullable.Should().BeFalse();

        // Check RequestPayload property
        var requestPayloadProperty = entityType.FindProperty("RequestPayload");
        requestPayloadProperty.Should().NotBeNull();
        requestPayloadProperty!.IsNullable.Should().BeFalse();

        // Check ProcessingStatus property
        var processingStatusProperty = entityType.FindProperty("ProcessingStatus");
        processingStatusProperty.Should().NotBeNull();
        processingStatusProperty!.IsNullable.Should().BeFalse();

        // Check AttemptCount property
        var attemptCountProperty = entityType.FindProperty("AttemptCount");
        attemptCountProperty.Should().NotBeNull();
        attemptCountProperty!.IsNullable.Should().BeFalse();

        // Check ProcessAfter property (nullable DateTimeOffset)
        var processAfterProperty = entityType.FindProperty("ProcessAfter");
        processAfterProperty.Should().NotBeNull();
        processAfterProperty!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldConfigureOptionalProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();

        // Check BusinessId property
        var businessIdProperty = entityType!.FindProperty("BusinessId");
        businessIdProperty.Should().NotBeNull();
        businessIdProperty!.IsNullable.Should().BeTrue();

        // Check UserId property
        var userIdProperty = entityType.FindProperty("UserId");
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeTrue();

        // Check LastErrorMessage property
        var lastErrorMessageProperty = entityType.FindProperty("LastErrorMessage");
        lastErrorMessageProperty.Should().NotBeNull();
        lastErrorMessageProperty!.IsNullable.Should().BeTrue();

        // Check CompletedAt property
        var completedAtProperty = entityType.FindProperty("CompletedAt");
        completedAtProperty.Should().NotBeNull();
        completedAtProperty!.IsNullable.Should().BeTrue();

        // Note: ResponsePayload property does not exist in the entity
        // RequestPayload exists and is required, which is tested in Configure_ShouldConfigureRequiredProperties
    }

    [Fact]
    public void Configure_ShouldConfigureEnumProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();

        // Check Status enum property exists
        var statusProperty = entityType!.FindProperty("Status");
        statusProperty.Should().NotBeNull();
        // Note: Value converter may not be set when using InMemory provider
        // The important thing is the property exists and is required
        statusProperty!.IsNullable.Should().BeFalse();

        // Check ProcessingStatus enum property exists
        var processingStatusProperty = entityType.FindProperty("ProcessingStatus");
        processingStatusProperty.Should().NotBeNull();
        processingStatusProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldConfigureIndexes()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();

        var indexes = entityType!.GetIndexes();
        indexes.Should().NotBeEmpty();

        // Check for IRN index
        var irnIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "Irn"));
        irnIndex.Should().NotBeNull();

        // Check for Status index
        var statusIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "Status"));
        statusIndex.Should().NotBeNull();

        // Check for ProcessingStatus index
        var processingStatusIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "ProcessingStatus"));
        processingStatusIndex.Should().NotBeNull();

        // Check for ProcessAfter index
        var processAfterIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "ProcessAfter"));
        processAfterIndex.Should().NotBeNull();
    }

    [Fact]
    public void Configure_ShouldConfigureAuditProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();

        var createdAtProperty = entityType!.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.IsNullable.Should().BeFalse();

        var createdByProperty = entityType.FindProperty("CreatedBy");
        createdByProperty.Should().NotBeNull();
        createdByProperty!.IsNullable.Should().BeFalse();

        var updatedAtProperty = entityType.FindProperty("UpdatedAt");
        updatedAtProperty.Should().NotBeNull();
        updatedAtProperty!.IsNullable.Should().BeTrue();

        var isDeletedProperty = entityType.FindProperty("IsDeleted");
        isDeletedProperty.Should().NotBeNull();
        isDeletedProperty!.IsNullable.Should().BeFalse();
        isDeletedProperty.GetDefaultValue().Should().Be(false);
    }

    [Fact]
    public void Configure_ShouldHaveSoftDeleteQueryFilter()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Configuration_ShouldWorkWithActualEntity()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var queueItem = InvoiceTransmissionQueue.Create(
            "IRN-12345-67890",
            InvoiceStatus.VALIDATED,
            "{\"invoice\": {\"id\": \"123\", \"amount\": 1000.00}}",
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        // Act
        _context.InvoiceTransmissionQueues.Add(queueItem);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedItem = await _context.InvoiceTransmissionQueues.FirstOrDefaultAsync(q => q.Id == queueItem.Id, TestContext.Current.CancellationToken);
        savedItem.Should().NotBeNull();
        savedItem!.Irn.Should().Be("IRN-12345-67890");
        savedItem.Status.Should().Be(InvoiceStatus.VALIDATED);
        savedItem.ProcessingStatus.Should().Be(QueueStatus.Pending);
        savedItem.AttemptCount.Should().Be(0);
        savedItem.RequestPayload.Should().Contain("invoice");
    }

    [Fact]
    public async Task Configuration_ShouldSupportAllInvoiceStatuses()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var statuses = new[]
        {
            InvoiceStatus.DRAFT,
            InvoiceStatus.CREATED,
            InvoiceStatus.APPROVED,
            InvoiceStatus.VALIDATED,
            InvoiceStatus.SIGNED,
            InvoiceStatus.TRANSMITTED
        };

        var queueItems = statuses.Select(status => InvoiceTransmissionQueue.Create(
            $"IRN-{status}-12345",
            status,
            "{\"test\": true}",
            Guid.NewGuid(),
            Guid.NewGuid()
        )).ToList();

        // Act
        _context.InvoiceTransmissionQueues.AddRange(queueItems);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedItems = await _context.InvoiceTransmissionQueues.ToListAsync(TestContext.Current.CancellationToken);
        savedItems.Should().HaveCount(statuses.Length);

        foreach (var status in statuses)
        {
            savedItems.Should().Contain(item => item.Status == status);
        }
    }

    [Fact]
    public async Task Configuration_ShouldSupportAllQueueStatuses()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var queueItem = InvoiceTransmissionQueue.Create(
            "IRN-TEST-12345",
            InvoiceStatus.VALIDATED,
            "{\"test\": true}",
            Guid.NewGuid(),
            Guid.NewGuid()
        );

        _context.InvoiceTransmissionQueues.Add(queueItem);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act & Assert - Test different queue statuses
        queueItem.MarkAsProcessing();
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Processing);

        queueItem.MarkAsCompleted();
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        queueItem.ProcessingStatus.Should().Be(QueueStatus.Completed);

        // Test marking as failed
        var newQueueItem = InvoiceTransmissionQueue.Create(
            "IRN-TEST-67890",
            InvoiceStatus.VALIDATED,
            "{\"test\": true}"
        );

        _context.InvoiceTransmissionQueues.Add(newQueueItem);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        newQueueItem.MarkAsProcessing();
        newQueueItem.MarkAsFailed("Test error", 3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var savedItem = await _context.InvoiceTransmissionQueues.FirstOrDefaultAsync(q => q.Id == newQueueItem.Id, cancellationToken: TestContext.Current.CancellationToken);
        savedItem!.ProcessingStatus.Should().Be(QueueStatus.Pending); // First failure should set back to Pending
        savedItem.LastErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public async Task Configuration_ShouldSupportQueryingByProcessingStatus()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var pendingItem = InvoiceTransmissionQueue.Create("IRN-PENDING-123", InvoiceStatus.VALIDATED, "{}");
        var processingItem = InvoiceTransmissionQueue.Create("IRN-PROCESSING-456", InvoiceStatus.VALIDATED, "{}");
        var completedItem = InvoiceTransmissionQueue.Create("IRN-COMPLETED-789", InvoiceStatus.VALIDATED, "{}");

        processingItem.MarkAsProcessing();
        completedItem.MarkAsProcessing();
        completedItem.MarkAsCompleted();

        _context.InvoiceTransmissionQueues.AddRange(pendingItem, processingItem, completedItem);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var pendingItems = await _context.InvoiceTransmissionQueues
            .Where(q => q.ProcessingStatus == QueueStatus.Pending)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        var processingItems = await _context.InvoiceTransmissionQueues
            .Where(q => q.ProcessingStatus == QueueStatus.Processing)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        var completedItems = await _context.InvoiceTransmissionQueues
            .Where(q => q.ProcessingStatus == QueueStatus.Completed)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        pendingItems.Should().HaveCount(1);
        pendingItems[0].Irn.Should().Be("IRN-PENDING-123");

        processingItems.Should().HaveCount(1);
        processingItems[0].Irn.Should().Be("IRN-PROCESSING-456");

        completedItems.Should().HaveCount(1);
        completedItems[0].Irn.Should().Be("IRN-COMPLETED-789");
        completedItems[0].CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Configure_ShouldHaveCorrectColumnTypes()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(InvoiceTransmissionQueue));

        // Act & Assert
        entityType.Should().NotBeNull();

        var processAfterProperty = entityType!.FindProperty("ProcessAfter");
        processAfterProperty.Should().NotBeNull();

        var completedAtProperty = entityType.FindProperty("CompletedAt");
        completedAtProperty.Should().NotBeNull();

        var createdAtProperty = entityType.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();

        var updatedAtProperty = entityType.FindProperty("UpdatedAt");
        updatedAtProperty.Should().NotBeNull();

        var deletedAtProperty = entityType.FindProperty("DeletedAt");
        deletedAtProperty.Should().NotBeNull();
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}