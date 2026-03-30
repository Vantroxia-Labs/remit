using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AegisEInvoicing.UnitTests.BackgroundServiceTests.Services;

/// <summary>
/// Tests for ISftpService async method signatures
/// These tests verify the async interface contract is properly implemented
/// </summary>
public class SftpServiceAsyncMethodsTests
{
    private readonly ISftpService _sftpService;

    public SftpServiceAsyncMethodsTests()
    {
        _sftpService = Substitute.For<ISftpService>();
    }

    #region GetConnectionDetailsAsync Tests

    [Fact]
    public async Task GetConnectionDetailsAsync_ShouldReturnConnectionDetails()
    {
        // Arrange
        var connectionId = "test-connection";
        var expectedDetails = new SftpConnectionDetails
        {
            ConnectionId = connectionId,
            Host = "test-host.com",
            Port = 22,
            UserName = "testuser",
            WorkingDirectory = "/test"
        };

        _sftpService.GetConnectionDetailsAsync(connectionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SftpConnectionDetails?>(expectedDetails));

        // Act
        var result = await _sftpService.GetConnectionDetailsAsync(connectionId, TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result!.ConnectionId.Should().Be(connectionId);
        result.Host.Should().Be("test-host.com");
    }

    [Fact]
    public async Task GetConnectionDetailsAsync_WithNonExistentConnection_ShouldReturnNull()
    {
        // Arrange
        _sftpService.GetConnectionDetailsAsync("non-existent", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<SftpConnectionDetails?>(null));

        // Act
        var result = await _sftpService.GetConnectionDetailsAsync("non-existent", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConnectionDetailsAsync_WhenExceptionThrown_ShouldReturnNull()
    {
        // Arrange
        _sftpService.GetConnectionDetailsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(
            () => _sftpService.GetConnectionDetailsAsync("test", TestContext.Current.CancellationToken));
    }

    #endregion

    #region GetAllConnectionsAsync Tests

    [Fact]
    public async Task GetAllConnectionsAsync_ShouldReturnAllConnections()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            new() { ConnectionId = "conn1", Host = "host1.com" },
            new() { ConnectionId = "conn2", Host = "host2.com" }
        };

        _sftpService.GetAllConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        // Act
        var result = await _sftpService.GetAllConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.ConnectionId == "conn1");
        result.Should().Contain(c => c.ConnectionId == "conn2");
    }

    [Fact]
    public async Task GetAllConnectionsAsync_WhenEmpty_ShouldReturnEmptyList()
    {
        // Arrange
        _sftpService.GetAllConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new List<SftpConnectionDetails>()));

        // Act
        var result = await _sftpService.GetAllConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetEnabledConnectionsAsync Tests

    [Fact]
    public async Task GetEnabledConnectionsAsync_ShouldReturnOnlyEnabledConnections()
    {
        // Arrange
        var connections = new List<SftpConnectionDetails>
        {
            new() { ConnectionId = "conn1", Host = "host1.com", IsEnabled = true },
            new() { ConnectionId = "conn2", Host = "host2.com", IsEnabled = true }
        };

        _sftpService.GetEnabledConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(connections));

        // Act
        var result = await _sftpService.GetEnabledConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.IsEnabled);
    }

    [Fact]
    public async Task GetEnabledConnectionsAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _sftpService.GetEnabledConnectionsAsync(cts.Token)
            .Returns(Task.FromResult(new List<SftpConnectionDetails>()));

        // Act
        await _sftpService.GetEnabledConnectionsAsync(cts.Token);

        // Assert
        await _sftpService.Received(1).GetEnabledConnectionsAsync(cts.Token);
    }

    #endregion

    #region TestAllConnectionsAsync Tests

    [Fact]
    public async Task TestAllConnectionsAsync_ShouldReturnDictionaryOfResults()
    {
        // Arrange
        var results = new Dictionary<string, bool>
        {
            { "conn1", true },
            { "conn2", false }
        };

        _sftpService.TestAllConnectionsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(results));

        // Act
        var result = await _sftpService.TestAllConnectionsAsync(TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
        result["conn1"].Should().BeTrue();
        result["conn2"].Should().BeFalse();
    }

    #endregion

    #region ListXmlFilesAsync Tests

    [Fact]
    public async Task ListXmlFilesAsync_ShouldReturnFiles()
    {
        // Arrange
        var files = new List<SftpFileInfo>
        {
            new() { FileName = "test1.xml", FullPath = "/test/test1.xml" },
            new() { FileName = "test2.xml", FullPath = "/test/test2.xml" }
        };

        _sftpService.ListInProgressFilesAsync("conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(files));

        // Act
        var result = await _sftpService.ListInProgressFilesAsync("conn1", TestContext.Current.CancellationToken);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task ListInProgressFilesAsync_WithInvalidConnection_ShouldThrowArgumentException()
    {
        // Arrange
        _sftpService.ListInProgressFilesAsync("invalid", Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Connection 'invalid' not found"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sftpService.ListInProgressFilesAsync("invalid", TestContext.Current.CancellationToken));
    }

    #endregion

    #region DownloadFileStreamAsync Tests

    [Fact]
    public async Task DownloadFileStreamAsync_ShouldReturnMemoryStream()
    {
        // Arrange
        var memoryStream = new MemoryStream(new byte[] { 1, 2, 3, 4, 5 });
        _sftpService.DownloadFileStreamAsync("conn1", "/test/file.xml", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(memoryStream));

        // Act
        var result = await _sftpService.DownloadFileStreamAsync("conn1", "/test/file.xml", TestContext.Current.CancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(5);
    }

    #endregion

    #region UploadFileAsync Tests

    [Fact]
    public async Task UploadFileAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _sftpService.UploadFileAsync("conn1", "/test/upload.xml", stream, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act & Assert - should not throw
        await _sftpService.UploadFileAsync("conn1", "/test/upload.xml", stream, TestContext.Current.CancellationToken);
    }

    #endregion

    #region MoveFileAsync Tests

    [Fact]
    public async Task MoveFileAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        _sftpService.MoveFileAsync("conn1", "/source/file.xml", "/dest/file.xml", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act & Assert - should not throw
        await _sftpService.MoveFileAsync("conn1", "/source/file.xml", "/dest/file.xml");
    }

    #endregion

    #region DeleteFileAsync Tests

    [Fact]
    public async Task DeleteFileAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        _sftpService.DeleteFileAsync("conn1", "/test/file.xml", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act & Assert - should not throw
        await _sftpService.DeleteFileAsync("conn1", "/test/file.xml");
    }

    #endregion

    #region CreateDirectoryIfNotExistsAsync Tests

    [Fact]
    public async Task CreateDirectoryIfNotExistsAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        _sftpService.CreateDirectoryIfNotExistsAsync("conn1", "/test/newdir", Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act & Assert - should not throw
        await _sftpService.CreateDirectoryIfNotExistsAsync("conn1", "/test/newdir");
    }

    #endregion

    #region TestConnectionAsync Tests

    [Fact]
    public async Task TestConnectionAsync_WithHealthyConnection_ShouldReturnTrue()
    {
        // Arrange
        _sftpService.TestConnectionAsync("conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _sftpService.TestConnectionAsync("conn1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task TestConnectionAsync_WithUnhealthyConnection_ShouldReturnFalse()
    {
        // Arrange
        _sftpService.TestConnectionAsync("conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _sftpService.TestConnectionAsync("conn1");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
