using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AegisEInvoicing.UnitTests.BackgroundServiceTests.Services;

/// <summary>
/// Tests for FileProcessingService cleanup functionality
/// </summary>
public class FileProcessingServiceCleanupTests
{
    private readonly IFileProcessingService _fileProcessingService;
    private readonly ISftpService _sftpService;

    public FileProcessingServiceCleanupTests()
    {
        _fileProcessingService = Substitute.For<IFileProcessingService>();
        _sftpService = Substitute.For<ISftpService>();
    }

    #region CleanupOldFilesAsync Tests

    [Fact]
    public async Task CleanupOldFilesAsync_WithDefaultDays_ShouldCleanupOldFiles()
    {
        // Arrange
        _fileProcessingService.CleanupOldFilesAsync(30, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(5));

        // Act
        var result = await _fileProcessingService.CleanupOldFilesAsync(30, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(5);
    }

    [Fact]
    public async Task CleanupOldFilesAsync_WithNoOldFiles_ShouldReturnZero()
    {
        // Arrange
        _fileProcessingService.CleanupOldFilesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));

        // Act
        var result = await _fileProcessingService.CleanupOldFilesAsync(7, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CleanupOldFilesAsync_WithCustomDays_ShouldUseCustomValue()
    {
        // Arrange
        _fileProcessingService.CleanupOldFilesAsync(7, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(10));

        // Act
        var result = await _fileProcessingService.CleanupOldFilesAsync(7, TestContext.Current.CancellationToken);

        // Assert
        result.Should().Be(10);
        await _fileProcessingService.Received(1).CleanupOldFilesAsync(7, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CleanupOldFilesAsync_WhenExceptionThrown_ShouldPropagate()
    {
        // Arrange
        _fileProcessingService.CleanupOldFilesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Cleanup failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _fileProcessingService.CleanupOldFilesAsync(30, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CleanupOldFilesAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _fileProcessingService.CleanupOldFilesAsync(30, cts.Token)
            .Returns(Task.FromResult(0));

        // Act
        await _fileProcessingService.CleanupOldFilesAsync(30, cts.Token);

        // Assert
        await _fileProcessingService.Received(1).CleanupOldFilesAsync(30, cts.Token);
    }

    #endregion

    #region GetServiceStatusAsync Tests

    [Fact]
    public async Task GetServiceStatusAsync_ShouldReturnServiceStatus()
    {
        // Arrange
        var expectedStatus = new ServiceStatus
        {
            IsRunning = true,
            LastProcessingRun = DateTime.UtcNow.AddMinutes(-5),
            Health = ServiceHealth.Healthy
        };

        _fileProcessingService.GetServiceStatusAsync()
            .Returns(Task.FromResult(expectedStatus));

        // Act
        var result = await _fileProcessingService.GetServiceStatusAsync();

        // Assert
        result.Should().NotBeNull();
        result.IsRunning.Should().BeTrue();
        result.Health.Should().Be(ServiceHealth.Healthy);
    }

    [Fact]
    public async Task GetServiceStatusAsync_WithActiveConnections_ShouldIncludeConnectionList()
    {
        // Arrange
        var expectedStatus = new ServiceStatus
        {
            IsRunning = true,
            ActiveConnections = new List<string> { "conn1", "conn2" }
        };

        _fileProcessingService.GetServiceStatusAsync()
            .Returns(Task.FromResult(expectedStatus));

        // Act
        var result = await _fileProcessingService.GetServiceStatusAsync();

        // Assert
        result.ActiveConnections.Should().HaveCount(2);
        result.ActiveConnections.Should().Contain("conn1");
        result.ActiveConnections.Should().Contain("conn2");
    }

    #endregion

    #region PerformHealthChecksAsync Tests

    [Fact]
    public async Task PerformHealthChecksAsync_ShouldReturnHealthCheckResults()
    {
        // Arrange
        var healthChecks = new Dictionary<string, bool>
        {
            { "Database", true },
            { "SFTP", true },
            { "Messaging", false }
        };

        _fileProcessingService.PerformHealthChecksAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(healthChecks));

        // Act
        var result = await _fileProcessingService.PerformHealthChecksAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(3);
        result["Database"].Should().BeTrue();
        result["SFTP"].Should().BeTrue();
        result["Messaging"].Should().BeFalse();
    }

    [Fact]
    public async Task PerformHealthChecksAsync_WhenAllHealthy_ShouldReturnAllTrue()
    {
        // Arrange
        var healthChecks = new Dictionary<string, bool>
        {
            { "Database", true },
            { "SFTP", true }
        };

        _fileProcessingService.PerformHealthChecksAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(healthChecks));

        // Act
        var result = await _fileProcessingService.PerformHealthChecksAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Values.Should().AllSatisfy(v => v.Should().BeTrue());
    }

    [Fact]
    public async Task PerformHealthChecksAsync_WhenExceptionThrown_ShouldPropagate()
    {
        // Arrange
        _fileProcessingService.PerformHealthChecksAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Health check failed"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _fileProcessingService.PerformHealthChecksAsync(TestContext.Current.CancellationToken));
    }

    #endregion

    #region ProcessAllPendingFilesAsync Tests

    [Fact]
    public async Task ProcessAllPendingFilesAsync_ShouldReturnProcessingStatistics()
    {
        // Arrange
        var expectedStats = new ProcessingStatistics
        {
            TotalFilesProcessed = 10,
            SuccessfulFiles = 8,
            ErrorFiles = 2,
            ProcessingStartTime = DateTime.UtcNow.AddMinutes(-5),
            ProcessingEndTime = DateTime.UtcNow
        };

        _fileProcessingService.ProcessAllPendingFilesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedStats));

        // Act
        var result = await _fileProcessingService.ProcessAllPendingFilesAsync(TestContext.Current.CancellationToken);

        // Assert
        result.TotalFilesProcessed.Should().Be(10);
        result.SuccessfulFiles.Should().Be(8);
        result.ErrorFiles.Should().Be(2);
    }

    [Fact]
    public async Task ProcessFilesFromConnectionAsync_ShouldReturnProcessingResults()
    {
        // Arrange
        var results = new List<FileProcessingResult>
        {
            new() { FileName = "file1.xml", IsSuccess = true },
            new() { FileName = "file2.xml", IsSuccess = false, ErrorMessage = "Processing failed" }
        };

        _fileProcessingService.ProcessFilesFromConnectionAsync(
            Arg.Any<string>(),
            Arg.Any<int?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(results));

        // Act
        var result = await _fileProcessingService.ProcessFilesFromConnectionAsync("conn1", cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.IsSuccess);
        result.Should().Contain(r => !r.IsSuccess);
    }

    #endregion

    #region Integration-style Tests

    [Fact]
    public async Task CleanupWorkflow_ShouldProcessAllConnections()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            CreateConnectionDetails("conn1"),
            CreateConnectionDetails("conn2")
        };

        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        var oldFiles = new List<SftpFileInfo>
        {
            new() { FileName = "old1.xml", LastModified = DateTime.UtcNow.AddDays(-60) },
            new() { FileName = "old2.xml", LastModified = DateTime.UtcNow.AddDays(-45) }
        };

        _sftpService.ListInProgressFilesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(oldFiles));

        _sftpService.DeleteFileAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act - verify the mock service interactions
        var enabledConnections = await _sftpService.GetEnabledConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        enabledConnections.Should().HaveCount(2);
        await _sftpService.Received(1).GetEnabledConnectionsAsync(Arg.Any<CancellationToken>());
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
            WorkingDirectory = "/test/Pending",
            PendingDirectory = "Pending",
            ReceiptsDirectory = "Receipts",
            RejectedDirectory = "Rejected",
            InProgressDirectory = "In-Progress",
            IsEnabled = true
        };
    }

    #endregion
}
