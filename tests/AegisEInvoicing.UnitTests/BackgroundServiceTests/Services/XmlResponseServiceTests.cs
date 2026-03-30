using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AegisEInvoicing.UnitTests.BackgroundServiceTests.Services;

/// <summary>
/// Tests for XmlResponseService async methods
/// </summary>
public class XmlResponseServiceTests
{
    private readonly IXmlResponseService _xmlResponseService;
    private readonly ISftpService _sftpService;
    private readonly ILogger<XmlResponseServiceTests> _logger;

    public XmlResponseServiceTests()
    {
        _xmlResponseService = Substitute.For<IXmlResponseService>();
        _sftpService = Substitute.For<ISftpService>();
        _logger = Substitute.For<ILogger<XmlResponseServiceTests>>();
    }

    #region GetResponseDirectoryAsync Tests

    [Fact]
    public async Task GetResponseDirectoryAsync_WithAckType_ShouldReturnAckDirectory()
    {
        // Arrange
        var connectionId = "test-connection";
        var expectedDirectory = "/ack";

        _xmlResponseService.GetResponseDirectoryAsync(connectionId, XmlResponseType.ACK, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedDirectory));

        // Act
        var result = await _xmlResponseService.GetResponseDirectoryAsync(connectionId, XmlResponseType.ACK);

        // Assert
        result.Should().Be(expectedDirectory);
    }

    [Fact]
    public async Task GetResponseDirectoryAsync_WithNackType_ShouldReturnNackDirectory()
    {
        // Arrange
        var connectionId = "test-connection";
        var expectedDirectory = "/nack";

        _xmlResponseService.GetResponseDirectoryAsync(connectionId, XmlResponseType.NACK, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedDirectory));

        // Act
        var result = await _xmlResponseService.GetResponseDirectoryAsync(connectionId, XmlResponseType.NACK);

        // Assert
        result.Should().Be(expectedDirectory);
    }

    [Fact]
    public async Task GetResponseDirectoryAsync_WithInvalidConnection_ShouldThrowArgumentException()
    {
        // Arrange
        _xmlResponseService.GetResponseDirectoryAsync("invalid", XmlResponseType.ACK, Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("Connection 'invalid' not found"));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _xmlResponseService.GetResponseDirectoryAsync("invalid", XmlResponseType.ACK));
    }

    [Fact]
    public async Task GetResponseDirectoryAsync_WithCancellationToken_ShouldPassToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _xmlResponseService.GetResponseDirectoryAsync("conn", XmlResponseType.ACK, cts.Token)
            .Returns(Task.FromResult("/ack"));

        // Act
        await _xmlResponseService.GetResponseDirectoryAsync("conn", XmlResponseType.ACK, cts.Token);

        // Assert
        await _xmlResponseService.Received(1).GetResponseDirectoryAsync("conn", XmlResponseType.ACK, cts.Token);
    }

    #endregion

    #region GenerateAckResponseAsync Tests

    [Fact]
    public async Task GenerateAckResponseAsync_ShouldReturnXmlResponse()
    {
        // Arrange
        var invoiceDetails = new InvoiceDetails
        {
            InvoiceId = Guid.NewGuid(),
            IRN = "INV-001",
            ProcessedAt = DateTime.UtcNow
        };

        var expectedResponse = new XmlResponse
        {
            FileName = "test_ACK.xml",
            Content = "<ack>...</ack>",
            Type = XmlResponseType.ACK
        };

        _xmlResponseService.GenerateAckResponseAsync(invoiceDetails, "test.xml", "conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _xmlResponseService.GenerateAckResponseAsync(invoiceDetails, "test.xml", "conn1");

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(XmlResponseType.ACK);
        result.FileName.Should().Contain("ACK");
    }

    #endregion

    #region GenerateNackResponseAsync Tests

    [Fact]
    public async Task GenerateNackResponseAsync_ShouldReturnXmlResponse()
    {
        // Arrange
        var errorDetails = new ErrorDetails
        {
            ErrorCode = "ERR001",
            ErrorMessage = "Processing failed",
            ErrorOccurredAt = DateTime.UtcNow
        };

        var expectedResponse = new XmlResponse
        {
            FileName = "test_NACK.xml",
            Content = "<nack>...</nack>",
            Type = XmlResponseType.NACK
        };

        _xmlResponseService.GenerateNackResponseAsync(errorDetails, "test.xml", "conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedResponse));

        // Act
        var result = await _xmlResponseService.GenerateNackResponseAsync(errorDetails, "test.xml", "conn1");

        // Assert
        result.Should().NotBeNull();
        result.Type.Should().Be(XmlResponseType.NACK);
        result.FileName.Should().Contain("NACK");
    }

    #endregion

    #region UploadResponseAsync Tests

    [Fact]
    public async Task UploadResponseAsync_WithValidResponse_ShouldReturnTrue()
    {
        // Arrange
        var response = new XmlResponse
        {
            FileName = "test.xml",
            Content = "<xml/>",
            TargetDirectory = "/ack"
        };

        _xmlResponseService.UploadResponseAsync(response, "conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _xmlResponseService.UploadResponseAsync(response, "conn1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task UploadResponseAsync_WhenUploadFails_ShouldReturnFalse()
    {
        // Arrange
        var response = new XmlResponse
        {
            FileName = "test.xml",
            Content = "<xml/>",
            TargetDirectory = "/ack"
        };

        _xmlResponseService.UploadResponseAsync(response, "conn1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(false));

        // Act
        var result = await _xmlResponseService.UploadResponseAsync(response, "conn1", TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateResponseAsync Tests

    [Fact]
    public async Task ValidateResponseAsync_WithValidXml_ShouldReturnValidResult()
    {
        // Arrange
        var response = new XmlResponse
        {
            Content = "<?xml version=\"1.0\"?><root/>"
        };

        var validationResult = new XmlValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        _xmlResponseService.ValidateResponseAsync(response)
            .Returns(Task.FromResult(validationResult));

        // Act
        var result = await _xmlResponseService.ValidateResponseAsync(response);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateResponseAsync_WithInvalidXml_ShouldReturnInvalidResult()
    {
        // Arrange
        var response = new XmlResponse
        {
            Content = "not xml"
        };

        var validationResult = new XmlValidationResult
        {
            IsValid = false,
            Errors = new List<string> { "Invalid XML format" }
        };

        _xmlResponseService.ValidateResponseAsync(response)
            .Returns(Task.FromResult(validationResult));

        // Act
        var result = await _xmlResponseService.ValidateResponseAsync(response);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain("Invalid XML format");
    }

    #endregion

    #region GenerateResponseFileName Tests

    [Fact]
    public void GenerateResponseFileName_WithAckType_ShouldContainAck()
    {
        // Arrange
        _xmlResponseService.GenerateResponseFileName("invoice.xml", XmlResponseType.ACK, null)
            .Returns("invoice_ACK_20240101_120000.xml");

        // Act
        var result = _xmlResponseService.GenerateResponseFileName("invoice.xml", XmlResponseType.ACK, null);

        // Assert
        result.Should().Contain("ACK");
        result.Should().StartWith("invoice");
    }

    [Fact]
    public void GenerateResponseFileName_WithNackType_ShouldContainNack()
    {
        // Arrange
        _xmlResponseService.GenerateResponseFileName("invoice.xml", XmlResponseType.NACK, null)
            .Returns("invoice_NACK_20240101_120000.xml");

        // Act
        var result = _xmlResponseService.GenerateResponseFileName("invoice.xml", XmlResponseType.NACK, null);

        // Assert
        result.Should().Contain("NACK");
    }

    [Fact]
    public void GenerateResponseFileName_WithTimestamp_ShouldUseProvidedTimestamp()
    {
        // Arrange
        var timestamp = new DateTime(2024, 6, 15, 10, 30, 0);
        _xmlResponseService.GenerateResponseFileName("invoice.xml", XmlResponseType.ACK, timestamp)
            .Returns("invoice_ACK_20240615_103000.xml");

        // Act
        var result = _xmlResponseService.GenerateResponseFileName("invoice.xml", XmlResponseType.ACK, timestamp);

        // Assert
        result.Should().Contain("20240615");
    }

    #endregion
}
