using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests;

public class ApplicationDbContextTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly Guid _testBusinessId = Guid.NewGuid();

    public ApplicationDbContextTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _dateTimeMock = new Mock<IDateTime>();
        _dateTimeMock.Setup(x => x.Now).Returns(DateTime.UtcNow);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeMock.Object);
    }

    private ApiUsageTracking CreateTestEntity(string endpoint = "/api/test")
    {
        return ApiUsageTracking.Create(
            _testBusinessId,
            endpoint,
            "GET",
            DateTimeOffset.UtcNow);
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateContext()
    {
        // Assert
        _context.Should().NotBeNull();
        _context.Database.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new ApplicationDbContext(
            null!,
            _currentUserServiceMock.Object,
            _dateTimeMock.Object);

        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullCurrentUserService_ShouldNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert - The implementation doesn't validate null services in constructor
        var action = () => new ApplicationDbContext(options, null!, _dateTimeMock.Object);
        action.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithNullDateTime_ShouldNotThrow()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        // Act & Assert - The implementation doesn't validate null services in constructor
        var action = () => new ApplicationDbContext(options, _currentUserServiceMock.Object, null!);
        action.Should().NotThrow();
    }

    [Fact]
    public void DbSets_ShouldBeConfiguredCorrectly()
    {
        // Assert
        _context.OutboxEvents.Should().NotBeNull();
        _context.IntegrationLogs.Should().NotBeNull();
        _context.Businesses.Should().NotBeNull();
        _context.Invoices.Should().NotBeNull();
        _context.InvoiceItems.Should().NotBeNull();
        _context.InvoiceApprovalHistories.Should().NotBeNull();
        _context.Parties.Should().NotBeNull();
        _context.BusinessItems.Should().NotBeNull();
        _context.ItemCategories.Should().NotBeNull();
        _context.Branches.Should().NotBeNull();
        _context.FlowRules.Should().NotBeNull();
        _context.FIRSApiConfigurations.Should().NotBeNull();
        _context.BusinessFIRSApiConfigurations.Should().NotBeNull();
        _context.BusinessOnboardings.Should().NotBeNull();
        _context.SystemConfigurations.Should().NotBeNull();
        _context.SubscriptionKeys.Should().NotBeNull();
        _context.ApiUsageTrackings.Should().NotBeNull();
        _context.ApiUsageSummaries.Should().NotBeNull();
        _context.InvoiceTransmissionQueues.Should().NotBeNull();
        _context.Users.Should().NotBeNull();
        _context.PlatformRoles.Should().NotBeNull();
        _context.UserRoleAssignments.Should().NotBeNull();
        _context.UserSessions.Should().NotBeNull();
        _context.RefreshTokens.Should().NotBeNull();
        _context.Subscriptions.Should().NotBeNull();
        _context.PlatformSubscriptions.Should().NotBeNull();
        _context.SFTPUsers.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutCurrentUser_ShouldUseDefaultUserId()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        var testEntity = CreateTestEntity("/api/without-user");

        // Act
        _context.ApiUsageTrackings.Add(testEntity);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        testEntity.CreatedBy.Should().Be(Guid.Parse("9c17ea5c-483c-44f8-97e8-c364e6739949"));
        testEntity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_WithCurrentUser_ShouldSetCreatedBy()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var testEntity = CreateTestEntity("/api/with-user");

        // Act
        _context.ApiUsageTrackings.Add(testEntity);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        testEntity.CreatedBy.Should().Be(userId);
        testEntity.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_OnUpdate_ShouldSetUpdatedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var testEntity = CreateTestEntity("/api/update-test");

        _context.ApiUsageTrackings.Add(testEntity);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Modify entity
        testEntity.RecordResponse(200, 100, 1000, 2000);
        _context.ApiUsageTrackings.Update(testEntity);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        testEntity.UpdatedBy.Should().Be(userId);
        testEntity.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_OnDelete_ShouldSoftDelete()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        var testEntity = CreateTestEntity("/api/delete-test");

        _context.ApiUsageTrackings.Add(testEntity);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Delete entity
        _context.ApiUsageTrackings.Remove(testEntity);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        testEntity.IsDeleted.Should().BeTrue();
        testEntity.DeletedBy.Should().Be(userId);
        testEntity.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldReturnTransaction()
    {
        // Act
        using var transaction = await _context.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        transaction.Should().NotBeNull();
        transaction.TransactionId.Should().NotBeEmpty();
    }

    [Fact]
    public void Model_ShouldApplyConfigurationsFromAssembly()
    {
        // Act
        var model = _context.Model;

        // Assert
        model.Should().NotBeNull();

        // Verify that some key entity types are configured
        var businessEntityType = model.FindEntityType(typeof(Business));
        businessEntityType.Should().NotBeNull();

        var outboxEventEntityType = model.FindEntityType(typeof(OutboxEvent));
        outboxEventEntityType.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var testEntity = CreateTestEntity("/api/cancel-test");
        _context.ApiUsageTrackings.Add(testEntity);

        // Act - InMemory provider doesn't respect cancellation, so just verify it accepts the token
        var result = await _context.SaveChangesAsync(cts.Token);

        // Assert
        result.Should().Be(1);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
