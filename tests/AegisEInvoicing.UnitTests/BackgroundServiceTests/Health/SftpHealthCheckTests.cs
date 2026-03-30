using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Health;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AegisEInvoicing.UnitTests.BackgroundServiceTests.Health;

/// <summary>
/// Comprehensive tests for SftpHealthCheck
/// </summary>
public class SftpHealthCheckTests
{
    private readonly ISftpService _sftpService;
    private readonly ILogger<SftpHealthCheck> _logger;
    private readonly SftpHealthCheck _healthCheck;

    public SftpHealthCheckTests()
    {
        _sftpService = Substitute.For<ISftpService>();
        _logger = Substitute.For<ILogger<SftpHealthCheck>>();
        _healthCheck = new SftpHealthCheck(_sftpService, _logger);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSftpService_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new SftpHealthCheck(null!, _logger);
        action.Should().Throw<ArgumentNullException>().WithParameterName("sftpService");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => new SftpHealthCheck(_sftpService, null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region CheckHealthAsync Tests

    [Fact]
    public async Task CheckHealthAsync_WithNoEnabledConnections_ShouldReturnUnhealthy()
    {
        // Arrange
        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<SftpConnectionDetails>()));

        // Act
        var result = await _healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("No SFTP connections are enabled");
    }

    [Fact]
    public async Task CheckHealthAsync_WithAllHealthyConnections_ShouldReturnHealthy()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            CreateConnectionDetails("conn1"),
            CreateConnectionDetails("conn2")
        };

        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        _sftpService.TestConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("All 2 SFTP connections are healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WithSomeUnhealthyConnections_ShouldReturnDegraded()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            CreateConnectionDetails("conn1"),
            CreateConnectionDetails("conn2")
        };

        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        _sftpService.TestConnectionAsync("conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));
        _sftpService.TestConnectionAsync("conn2", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Contain("1/2 SFTP connections are healthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WithAllUnhealthyConnections_ShouldReturnUnhealthy()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            CreateConnectionDetails("conn1"),
            CreateConnectionDetails("conn2")
        };

        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        _sftpService.TestConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("All SFTP connections are unhealthy");
    }

    [Fact]
    public async Task CheckHealthAsync_WhenConnectionTestThrows_ShouldTreatAsUnhealthy()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            CreateConnectionDetails("conn1")
        };

        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        _sftpService.TestConnectionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Connection test failed"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenGetEnabledConnectionsThrows_ShouldReturnUnhealthy()
    {
        // Arrange
        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Health check failed");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeConnectionDetailsInData()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            CreateConnectionDetails("conn1")
        };

        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        _sftpService.TestConnectionAsync("conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _healthCheck.CheckHealthAsync(null!, CancellationToken.None);

        // Assert
        result.Data.Should().ContainKey("TotalConnections");
        result.Data.Should().ContainKey("HealthyConnections");
        result.Data.Should().ContainKey("UnhealthyConnections");
        result.Data.Should().ContainKey("ConnectionDetails");
        result.Data["TotalConnections"].Should().Be(1);
        result.Data["HealthyConnections"].Should().Be(1);
    }

    #endregion

    #region Helper Methods

    private static SftpConnectionDetails CreateConnectionDetails(string connectionId)
    {
        return new SftpConnectionDetails
        {
            ConnectionId = connectionId,
            Host = "test-host.com",
            Port = 22,
            UserName = "testuser",
            WorkingDirectory = "/test",
            IsEnabled = true
        };
    }

    #endregion
}
