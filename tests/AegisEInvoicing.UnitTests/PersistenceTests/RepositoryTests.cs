using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Persistence;
using AegisEInvoicing.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;
using System.Linq.Expressions;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests;

public class RepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Repository<ApiUsageTracking> _repository;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTime> _dateTimeMock;
    private readonly Guid _testBusinessId = Guid.NewGuid();

    public RepositoryTests()
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
        _repository = new Repository<ApiUsageTracking>(_context);
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
    public void Constructor_WithValidContext_ShouldCreateRepository()
    {
        // Assert
        _repository.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new Repository<ApiUsageTracking>(null!);
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("context");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnEntity()
    {
        // Arrange
        var entity = CreateTestEntity("/api/invoices");
        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetByIdAsync(entity.Id, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(entity.Id);
        result.Endpoint.Should().Be("/api/invoices");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentId_ShouldReturnNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - Call method with cancellation token (InMemory provider doesn't actually respect cancellation)
        var result = await _repository.GetByIdAsync(Guid.NewGuid(), cts.Token);

        // Assert - Method should complete and return null for non-existent ID
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WithEmptyRepository_ShouldReturnEmptyCollection()
    {
        // Act
        var result = await _repository.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ShouldReturnAllEntities()
    {
        // Arrange
        var entity1 = CreateTestEntity("/api/entity1");
        var entity2 = CreateTestEntity("/api/entity2");

        await _repository.AddAsync(entity1, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity2, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetAllAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(e => e.Endpoint == "/api/entity1");
        result.Should().Contain(e => e.Endpoint == "/api/entity2");
    }

    [Fact]
    public async Task GetAllAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - Call method with cancellation token (InMemory provider doesn't actually respect cancellation)
        var result = await _repository.GetAllAsync(cts.Token);

        // Assert - Method should complete and return empty collection
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_WithMatchingPredicate_ShouldReturnMatchingEntities()
    {
        // Arrange
        var entity1 = CreateTestEntity("/api/test");
        var entity2 = CreateTestEntity("/api/another");
        var entity3 = CreateTestEntity("/api/test");

        await _repository.AddAsync(entity1, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity2, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity3, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.FindAsync(e => e.Endpoint == "/api/test", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(e => e.Endpoint == "/api/test");
    }

    [Fact]
    public async Task FindAsync_WithNonMatchingPredicate_ShouldReturnEmptyCollection()
    {
        // Arrange
        var entity = CreateTestEntity("/api/test");
        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.FindAsync(e => e.Endpoint == "/api/nonexistent", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        Expression<Func<ApiUsageTracking, bool>> predicate = e => e.Endpoint == "/api/test";

        // Act - Call method with cancellation token (InMemory provider doesn't actually respect cancellation)
        var result = await _repository.FindAsync(predicate, cts.Token);

        // Assert - Method should complete and return empty collection
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AddAsync_WithValidEntity_ShouldAddEntity()
    {
        // Arrange
        var entity = CreateTestEntity("/api/new");

        // Act
        var result = await _repository.AddAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(entity);
        result.Id.Should().NotBeEmpty();

        // Verify entity is tracked
        _context.Entry(entity).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task AddAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        var entity = CreateTestEntity("/api/test");
        using var cts = new CancellationTokenSource();

        // Act - Call method with cancellation token (InMemory provider doesn't actually respect cancellation)
        var result = await _repository.AddAsync(entity, cts.Token);

        // Assert - Method should complete and return the entity
        result.Should().Be(entity);
        result.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_ShouldUpdateEntity()
    {
        // Arrange
        var entity = CreateTestEntity("/api/original");
        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Modify entity via recording response (to change state)
        entity.RecordResponse(200, 100, 1000, 2000);

        // Act
        await _repository.UpdateAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        _context.Entry(entity).State.Should().Be(EntityState.Modified);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_ShouldMarkForDeletion()
    {
        // Arrange
        var entity = CreateTestEntity("/api/todelete");
        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _repository.DeleteAsync(entity, TestContext.Current.CancellationToken);

        // Assert
        _context.Entry(entity).State.Should().Be(EntityState.Deleted);
    }

    [Fact]
    public async Task CountAsync_WithoutPredicate_ShouldReturnTotalCount()
    {
        // Arrange
        var entity1 = CreateTestEntity("/api/entity1");
        var entity2 = CreateTestEntity("/api/entity2");

        await _repository.AddAsync(entity1, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity2, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var count = await _repository.CountAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ShouldReturnFilteredCount()
    {
        // Arrange
        var entity1 = CreateTestEntity("/api/test");
        var entity2 = CreateTestEntity("/api/another");
        var entity3 = CreateTestEntity("/api/test");

        await _repository.AddAsync(entity1, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity2, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity3, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var count = await _repository.CountAsync(e => e.Endpoint == "/api/test", TestContext.Current.CancellationToken);

        // Assert
        count.Should().Be(2);
    }

    [Fact]
    public async Task CountAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        // Act - Call method with cancellation token (InMemory provider doesn't actually respect cancellation)
        var count = await _repository.CountAsync(null, cts.Token);

        // Assert - Method should complete and return 0
        count.Should().Be(0);
    }

    [Fact]
    public async Task AnyAsync_WithMatchingPredicate_ShouldReturnTrue()
    {
        // Arrange
        var entity = CreateTestEntity("/api/test");
        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var exists = await _repository.AnyAsync(e => e.Endpoint == "/api/test", TestContext.Current.CancellationToken);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task AnyAsync_WithNonMatchingPredicate_ShouldReturnFalse()
    {
        // Arrange
        var entity = CreateTestEntity("/api/test");
        await _repository.AddAsync(entity, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var exists = await _repository.AnyAsync(e => e.Endpoint == "/api/nonexistent", TestContext.Current.CancellationToken);

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task AnyAsync_WithCancellation_ShouldAcceptCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        Expression<Func<ApiUsageTracking, bool>> predicate = e => e.Endpoint == "/api/test";

        // Act - Call method with cancellation token (InMemory provider doesn't actually respect cancellation)
        var exists = await _repository.AnyAsync(predicate, cts.Token);

        // Assert - Method should complete and return false
        exists.Should().BeFalse();
    }

    [Fact]
    public void Query_ShouldReturnQueryable()
    {
        // Act
        var queryable = _repository.Query();

        // Assert
        queryable.Should().NotBeNull();
        queryable.Should().BeAssignableTo<IQueryable<ApiUsageTracking>>();
    }

    [Fact]
    public async Task Query_WithLinqOperations_ShouldWork()
    {
        // Arrange
        var entity1 = CreateTestEntity("/test/alpha");
        var entity2 = CreateTestEntity("/test/xyz");
        var entity3 = CreateTestEntity("/test/gamma");

        await _repository.AddAsync(entity1, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity2, TestContext.Current.CancellationToken);
        await _repository.AddAsync(entity3, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - Filter for endpoints ending with 'a' or 'pha' (alpha, gamma but not xyz)
        var result = await _repository.Query()
            .Where(e => e.Endpoint.EndsWith("a"))
            .OrderBy(e => e.Endpoint)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result[0].Endpoint.Should().Be("/test/alpha");
        result[1].Endpoint.Should().Be("/test/gamma");
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}