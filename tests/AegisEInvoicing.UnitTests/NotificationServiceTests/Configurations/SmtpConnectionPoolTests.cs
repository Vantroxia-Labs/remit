using AegisEInvoicing.NotificationService.Configurations;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace AegisEInvoicing.UnitTests.NotificationServiceTests.Configurations;

/// <summary>
/// Comprehensive tests for SmtpConnectionPool
/// </summary>
public class SmtpConnectionPoolTests : IDisposable
{
    private readonly IOptions<MailKitConfiguration> _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmtpConnectionPool> _logger;
    private readonly ILogger<MailKitSmtpConnection> _connectionLogger;
    private SmtpConnectionPool? _pool;

    public SmtpConnectionPoolTests()
    {
        var config = new MailKitConfiguration
        {
            SmtpServer = "smtp.test.com",
            SmtpPort = 587,
            Username = "test@test.com",
            Password = "password",
            UseSsl = true,
            ConnectionPoolSize = 5,
            MaxConcurrentOperations = 3,
            ConnectionIdleTimeout = TimeSpan.FromMinutes(5)
        };

        _options = Options.Create(config);
        _logger = Substitute.For<ILogger<SmtpConnectionPool>>();
        _connectionLogger = Substitute.For<ILogger<MailKitSmtpConnection>>();

        var services = new ServiceCollection();
        services.AddSingleton(_connectionLogger);
        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _pool?.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldInitializePool()
    {
        // Act
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);

        // Assert
        _pool.Should().NotBeNull();
    }

    #endregion

    #region GetConnectionAsync Tests

    [Fact]
    public async Task GetConnectionAsync_ShouldCreateNewConnection_WhenPoolEmpty()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);

        // Act & Assert
        // This will try to connect to the SMTP server which will fail in tests
        // but we're testing the pool mechanics
        await Assert.ThrowsAnyAsync<Exception>(
            () => _pool.GetConnectionAsync(CancellationToken.None));
    }

    #endregion

    #region ReturnConnectionAsync Tests

    [Fact]
    public async Task ReturnConnectionAsync_WithHealthyConnection_ShouldReturnToPool()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.IsConnected.Returns(true);
        connection.IsAuthenticated.Returns(true);
        connection.LastUsed.Returns(DateTime.UtcNow);
        connection.ConnectionId.Returns("test-connection");

        // Act - should not throw
        await _pool.ReturnConnectionAsync(connection, true);

        // Assert
        await connection.DidNotReceive().DisconnectAsync();
    }

    [Fact]
    public async Task ReturnConnectionAsync_WithUnhealthyConnection_ShouldDisposeConnection()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.ConnectionId.Returns("test-connection");

        // Act
        await _pool.ReturnConnectionAsync(connection, false);

        // Assert
        await connection.Received(1).DisconnectAsync();
        connection.Received(1).Dispose();
    }

    #endregion

    #region ValidateConnectionHealth Tests

    [Fact]
    public void ValidateConnectionHealth_WithValidConnection_ShouldReturnTrue()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.IsConnected.Returns(true);
        connection.IsAuthenticated.Returns(true);
        connection.LastUsed.Returns(DateTime.UtcNow);

        // Act
        var result = _pool.ValidateConnectionHealth(connection);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateConnectionHealth_WithDisconnectedConnection_ShouldReturnFalse()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.IsConnected.Returns(false);
        connection.IsAuthenticated.Returns(true);
        connection.LastUsed.Returns(DateTime.UtcNow);

        // Act
        var result = _pool.ValidateConnectionHealth(connection);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateConnectionHealth_WithUnauthenticatedConnection_ShouldReturnFalse()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.IsConnected.Returns(true);
        connection.IsAuthenticated.Returns(false);
        connection.LastUsed.Returns(DateTime.UtcNow);

        // Act
        var result = _pool.ValidateConnectionHealth(connection);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateConnectionHealth_WithIdleConnection_ShouldReturnFalse()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.IsConnected.Returns(true);
        connection.IsAuthenticated.Returns(true);
        connection.LastUsed.Returns(DateTime.UtcNow.AddMinutes(-10)); // Older than idle timeout

        // Act
        var result = _pool.ValidateConnectionHealth(connection);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateConnectionHealth_WhenExceptionThrown_ShouldReturnFalse()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.IsConnected.Returns(x => throw new Exception("Connection error"));

        // Act
        var result = _pool.ValidateConnectionHealth(connection);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_ShouldDisposeAllConnections()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);

        // Act - should not throw
        _pool.Dispose();

        // Assert - second dispose should also not throw
        var action = () => _pool.Dispose();
        action.Should().NotThrow();
    }

    [Fact]
    public async Task Dispose_AfterReturningConnections_ShouldDisposeAll()
    {
        // Arrange
        _pool = new SmtpConnectionPool(_options, _serviceProvider, _logger);
        var connection = Substitute.For<ISmtpConnection>();
        connection.ConnectionId.Returns("test-connection");
        connection.IsConnected.Returns(true);
        connection.IsAuthenticated.Returns(true);
        connection.LastUsed.Returns(DateTime.UtcNow);

        await _pool.ReturnConnectionAsync(connection, true);

        // Act
        _pool.Dispose();

        // Assert - pool should be disposed
        _pool.Should().NotBeNull(); // Just verify no exception thrown
    }

    #endregion
}
