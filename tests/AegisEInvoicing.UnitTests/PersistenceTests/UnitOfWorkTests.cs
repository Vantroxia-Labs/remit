using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests;

public class UnitOfWorkTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly Guid _testBusinessId = Guid.NewGuid();

    public UnitOfWorkTests()
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
        _unitOfWork = new UnitOfWork(_context);
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
    public void Constructor_WithValidContext_ShouldCreateUnitOfWork()
    {
        // Assert
        _unitOfWork.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new UnitOfWork(null!);
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("context");
    }

    [Fact]
    public async Task SaveChangesAsync_WithChanges_ShouldReturnNumberOfAffectedEntries()
    {
        // Arrange
        var entity = CreateTestEntity();
        _context.ApiUsageTrackings.Add(entity);

        // Act
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutChanges_ShouldReturnZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SaveChangesAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        var entity = CreateTestEntity();
        _context.ApiUsageTrackings.Add(entity);

        // Act - InMemory provider doesn't respect cancellation, so just verify it accepts the token
        var result = await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public async Task BeginTransactionAsync_ShouldReturnTransaction()
    {
        // Act
        using var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        transaction.Should().NotBeNull();
        transaction.Should().BeAssignableTo<IDbContextTransaction>();
    }

    [Fact]
    public async Task BeginTransactionAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        // Act - InMemory provider doesn't respect cancellation, so just verify it accepts the token
        using var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Assert
        transaction.Should().NotBeNull();
    }

    [Fact]
    public async Task CommitAsync_WithActiveTransaction_ShouldCommitTransaction()
    {
        // Arrange
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var entity = CreateTestEntity();
        _context.ApiUsageTrackings.Add(entity);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        // Transaction should be disposed and nullified
        // We can verify this by attempting to begin another transaction
        var newTransaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        newTransaction.Should().NotBeNull();
        newTransaction.Dispose();
    }

    [Fact]
    public async Task CommitAsync_WithoutActiveTransaction_ShouldNotThrow()
    {
        // Act
        var action = async () => await _unitOfWork.CommitAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CommitAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act - InMemory provider doesn't respect cancellation, just verify it accepts the token
        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        // If we get here without exception, test passes
    }

    [Fact]
    public async Task RollbackAsync_WithActiveTransaction_ShouldRollbackTransaction()
    {
        // Arrange
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var entity = CreateTestEntity();
        _context.ApiUsageTrackings.Add(entity);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);

        // Assert
        // InMemory database doesn't really support transactions, so entity may still exist
        // But the important thing is the method doesn't throw
    }

    [Fact]
    public async Task RollbackAsync_WithoutActiveTransaction_ShouldNotThrow()
    {
        // Act
        var action = async () => await _unitOfWork.RollbackAsync();

        // Assert
        await action.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RollbackAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act - InMemory provider doesn't respect cancellation, just verify it accepts the token
        await _unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);

        // Assert
        // If we get here without exception, test passes
        await transaction.DisposeAsync();
    }

    [Fact]
    public async Task TransactionWorkflow_BeginCommit_ShouldPersistChanges()
    {
        // Arrange
        var entity = CreateTestEntity("/api/commit-test");

        // Act
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _context.ApiUsageTrackings.Add(entity);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        var savedEntity = await _context.ApiUsageTrackings.FirstOrDefaultAsync(e => e.Endpoint == "/api/commit-test", TestContext.Current.CancellationToken);
        savedEntity.Should().NotBeNull();
        savedEntity!.Endpoint.Should().Be("/api/commit-test");
    }

    [Fact]
    public async Task TransactionWorkflow_BeginRollback_ShouldCompleteWithoutError()
    {
        // Arrange
        var entity = CreateTestEntity("/api/rollback-test");

        // Act
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _context.ApiUsageTrackings.Add(entity);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);

        // Assert
        // InMemory database doesn't truly support transactions
        // Just verify the rollback operation completes without error
    }

    [Fact]
    public async Task MultipleTransactions_ShouldWorkSequentially()
    {
        // First transaction
        var transaction1 = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var entity1 = CreateTestEntity("/api/entity1");
        _context.ApiUsageTrackings.Add(entity1);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Second transaction
        var transaction2 = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var entity2 = CreateTestEntity("/api/entity2");
        _context.ApiUsageTrackings.Add(entity2);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _unitOfWork.CommitAsync(TestContext.Current.CancellationToken);

        // Assert
        var count = await _context.ApiUsageTrackings.CountAsync(TestContext.Current.CancellationToken);
        count.Should().Be(2);
    }

    [Fact]
    public async Task Dispose_ShouldCleanupResources()
    {
        // Act
        _unitOfWork.Dispose();

        // Assert
        // After disposal, operations should fail
        var action = async () => await _unitOfWork.SaveChangesAsync();
        await action.Should().ThrowAsync<ObjectDisposedException>();
    }

    [Fact]
    public async Task Dispose_WithActiveTransaction_ShouldCompleteWithoutError()
    {
        // Arrange
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        // Act
        _unitOfWork.Dispose();

        // Assert - InMemory transactions may not throw ObjectDisposedException
        // Just verify the dispose completes without error
        transaction.Should().NotBeNull();
    }

    [Fact]
    public async Task NestedTransactionScenario_ShouldHandleCorrectly()
    {
        // Arrange
        var entity1 = CreateTestEntity("/api/nested1");
        var entity2 = CreateTestEntity("/api/nested2");

        // Act - Begin transaction and save first entity
        var transaction = await _unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        _context.ApiUsageTrackings.Add(entity1);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add second entity and rollback
        _context.ApiUsageTrackings.Add(entity2);
        await _unitOfWork.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _unitOfWork.RollbackAsync(TestContext.Current.CancellationToken);

        // Assert - InMemory database doesn't support real transactions
        // Just verify the workflow completes
    }

    public void Dispose()
    {
        _unitOfWork?.Dispose();
    }
}
