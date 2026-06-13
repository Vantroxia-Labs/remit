using AegisEInvoicing.Infrastructure.Services.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace AegisEInvoicing.UnitTests.InfrastructureTests.Services.Caching;

public class RedisCacheServiceTests : IDisposable
{
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly Mock<IDatabase> _databaseMock;
    private readonly Mock<IServer> _serverMock;
    private readonly IMemoryCache _memoryCache;
    private readonly Mock<ILogger<RedisCacheService>> _loggerMock;
    private readonly Mock<IOptions<CacheOptions>> _optionsMock;
    private readonly CacheOptions _cacheOptions;
    private readonly RedisCacheService _cacheService;

    public RedisCacheServiceTests()
    {
        _redisMock = new Mock<IConnectionMultiplexer>();
        _databaseMock = new Mock<IDatabase>();
        _serverMock = new Mock<IServer>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<RedisCacheService>>();
        _optionsMock = new Mock<IOptions<CacheOptions>>();

        _cacheOptions = new CacheOptions
        {
            DefaultExpirationMinutes = 5,
            SlidingExpirationMinutes = 2,
            UseDistributedCache = true
        };

        _optionsMock.Setup(x => x.Value).Returns(_cacheOptions);
        _redisMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_databaseMock.Object);
        _redisMock.Setup(x => x.IsConnected).Returns(true);

        _cacheService = new RedisCacheService(_redisMock.Object, _memoryCache, _optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullMemoryCache_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RedisCacheService(_redisMock.Object, null!, _optionsMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RedisCacheService(_redisMock.Object, _memoryCache, null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new RedisCacheService(_redisMock.Object, _memoryCache, _optionsMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithNullRedis_ShouldNotThrow()
    {
        // Act & Assert
        var service = new RedisCacheService(null, _memoryCache, _optionsMock.Object, _loggerMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAsync_WithNullOrWhitespaceKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _cacheService.Invoking(s => s.GetAsync<string>(null!))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.GetAsync<string>(""))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.GetAsync<string>("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAsync_WithRedisConnected_ShouldReturnValueFromRedis()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var serializedValue = System.Text.Json.JsonSerializer.Serialize(value);

        _databaseMock.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _cacheService.GetAsync<string>(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(value);
        _databaseMock.Verify(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WithRedisUnavailable_ShouldFallbackToMemoryCache()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        var serviceWithoutRedis = new RedisCacheService(null, _memoryCache, _optionsMock.Object, _loggerMock.Object);

        // Set value in memory cache directly
        _memoryCache.Set(key, value);

        // Act
        var result = await serviceWithoutRedis.GetAsync<string>(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task GetAsync_WithRedisMiss_ShouldCheckMemoryCache()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        _databaseMock.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        _memoryCache.Set(key, value);

        // Act
        var result = await _cacheService.GetAsync<string>(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task GetAsync_WithBothCacheMiss_ShouldReturnDefault()
    {
        // Arrange
        var key = "non-existent-key";

        _databaseMock.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _cacheService.GetAsync<string>(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_WithRedisException_ShouldFallbackToMemoryCache()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        _databaseMock.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Redis connection failed"));

        _memoryCache.Set(key, value);

        // Act
        var result = await _cacheService.GetAsync<string>(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithNullOrWhitespaceKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _cacheService.Invoking(s => s.SetAsync(null!, "value"))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.SetAsync("", "value"))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.SetAsync("   ", "value"))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task SetAsync_WithNullValue_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await _cacheService.Invoking(s => s.SetAsync("key", (string)null!))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SetAsync_WithValidData_ShouldSetInBothCaches()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        // Act
        await _cacheService.SetAsync(key, value, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            key,
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);

        _memoryCache.Get(key).Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_WithCustomExpiry_ShouldUseCustomExpiry()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var customExpiry = TimeSpan.FromHours(1);

        // Act
        await _cacheService.SetAsync(key, value, customExpiry, TestContext.Current.CancellationToken);

        // Assert
        _databaseMock.Verify(x => x.StringSetAsync(
            key,
            It.IsAny<RedisValue>(),
            customExpiry,
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WithRedisException_ShouldStillSetInMemoryCache()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        _databaseMock.Setup(x => x.StringSetAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<RedisValue>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Redis connection failed"));

        // Act
        await _cacheService.SetAsync(key, value, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        _memoryCache.Get(key).Should().Be(value);
    }

    [Fact]
    public async Task RemoveAsync_WithNullOrWhitespaceKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _cacheService.Invoking(s => s.RemoveAsync(null!))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.RemoveAsync(""))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.RemoveAsync("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RemoveAsync_WithValidKey_ShouldRemoveFromBothCaches()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        _memoryCache.Set(key, value);

        // Act
        await _cacheService.RemoveAsync(key, TestContext.Current.CancellationToken);

        // Assert
        _databaseMock.Verify(x => x.KeyDeleteAsync(key, It.IsAny<CommandFlags>()), Times.Once);
        _memoryCache.Get(key).Should().BeNull();
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithNullOrWhitespacePattern_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _cacheService.Invoking(s => s.RemoveByPatternAsync(null!))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.RemoveByPatternAsync(""))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.RemoveByPatternAsync("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RemoveByPatternAsync_WithValidPattern_ShouldRemoveMatchingKeys()
    {
        // Arrange
        var pattern = "test:*";
        var keys = new RedisKey[] { "test:1", "test:2", "test:3" };

        var endPoint = new System.Net.DnsEndPoint("localhost", 6379);
        _redisMock.Setup(x => x.GetEndPoints(It.IsAny<bool>())).Returns(new System.Net.EndPoint[] { endPoint });
        _redisMock.Setup(x => x.GetServer(endPoint, It.IsAny<object>())).Returns(_serverMock.Object);
        _serverMock.Setup(x => x.Keys(It.IsAny<int>(), pattern, It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
            .Returns(keys);

        // Act
        await _cacheService.RemoveByPatternAsync(pattern, TestContext.Current.CancellationToken);

        // Assert
        _databaseMock.Verify(x => x.KeyDeleteAsync(keys, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WithNullOrWhitespaceKey_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _cacheService.Invoking(s => s.ExistsAsync(null!))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.ExistsAsync(""))
            .Should().ThrowAsync<ArgumentException>();

        await _cacheService.Invoking(s => s.ExistsAsync("   "))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ExistsAsync_WithRedisConnected_ShouldCheckRedis()
    {
        // Arrange
        var key = "test-key";
        _databaseMock.Setup(x => x.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _cacheService.ExistsAsync(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        _databaseMock.Verify(x => x.KeyExistsAsync(key, It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WithRedisException_ShouldFallbackToMemoryCache()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";

        _databaseMock.Setup(x => x.KeyExistsAsync(key, It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisException("Redis connection failed"));

        _memoryCache.Set(key, value);

        // Act
        var result = await _cacheService.ExistsAsync(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetAsync_WithComplexObject_ShouldSerializeDeserializeCorrectly()
    {
        // Arrange
        var key = "complex-object";
        var value = new { Id = 123, Name = "Test", Items = new[] { "item1", "item2" } };
        var serializedValue = System.Text.Json.JsonSerializer.Serialize(value);

        _databaseMock.Setup(x => x.StringGetAsync(key, It.IsAny<CommandFlags>()))
            .ReturnsAsync(serializedValue);

        // Act
        var result = await _cacheService.GetAsync<object>(key, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    public void Dispose()
    {
        _memoryCache?.Dispose();
    }
}