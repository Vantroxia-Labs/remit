using AegisEInvoicing.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.InfrastructureTests.Services;

public class EncryptionServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<EncryptionService>> _loggerMock;
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<EncryptionService>>();

        // Setup valid encryption key and IV (32 bytes each for AES-256)
        var validKey = "12345678901234567890123456789012"; // 32 characters
        var validIv = "1234567890123456"; // 16 characters for IV

        _configurationMock.Setup(c => c["Encryption:Key"]).Returns(validKey);
        _configurationMock.Setup(c => c["Encryption:Iv"]).Returns(validIv);

        _encryptionService = new EncryptionService(_configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new EncryptionService(_configurationMock.Object, null!));
    }

    [Fact]
    public void Constructor_WithMissingEncryptionKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Encryption:Key"]).Returns((string)null!);
        configMock.Setup(c => c["ENCRYPTION_KEY"]).Returns((string)null!);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new EncryptionService(configMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithMissingEncryptionIv_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Encryption:Key"]).Returns("12345678901234567890123456789012");
        configMock.Setup(c => c["Encryption:Iv"]).Returns((string)null!);
        configMock.Setup(c => c["ENCRYPTION_IV"]).Returns((string)null!);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new EncryptionService(configMock.Object, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithEnvironmentVariableKey_ShouldWork()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Encryption:Key"]).Returns((string)null!);
        configMock.Setup(c => c["ENCRYPTION_KEY"]).Returns("12345678901234567890123456789012");
        configMock.Setup(c => c["Encryption:Iv"]).Returns((string)null!);
        configMock.Setup(c => c["ENCRYPTION_IV"]).Returns("1234567890123456");

        // Act & Assert
        var service = new EncryptionService(configMock.Object, _loggerMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task EncryptAsync_WithNullPlainText_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _encryptionService.Invoking(s => s.EncryptAsync(null!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Plain text cannot be null or empty*");
    }

    [Fact]
    public async Task EncryptAsync_WithEmptyPlainText_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _encryptionService.Invoking(s => s.EncryptAsync(""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Plain text cannot be null or empty*");
    }

    [Fact]
    public async Task EncryptAsync_WithValidPlainText_ShouldReturnEncryptedString()
    {
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var result = await _encryptionService.EncryptAsync(plainText);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().NotBe(plainText);
        // Encrypted result should be base64 encoded
        result.Should().MatchRegex("^[A-Za-z0-9+/]*={0,3}$");
    }

    [Fact]
    public async Task DecryptAsync_WithNullEncryptedText_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _encryptionService.Invoking(s => s.DecryptAsync(null!))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Encrypted text cannot be null or empty*");
    }

    [Fact]
    public async Task DecryptAsync_WithEmptyEncryptedText_ShouldThrowArgumentException()
    {
        // Act & Assert
        await _encryptionService.Invoking(s => s.DecryptAsync(""))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("Encrypted text cannot be null or empty*");
    }

    [Fact]
    public async Task DecryptAsync_WithInvalidEncryptedText_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        await _encryptionService.Invoking(s => s.DecryptAsync("invalid-encrypted-text"))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Failed to decrypt data*");
    }

    [Fact]
    public async Task EncryptDecrypt_RoundTrip_ShouldReturnOriginalText()
    {
        // Arrange
        var plainText = "This is a test message for encryption!";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptAsync_WithSamePlainText_ShouldReturnSameEncryptedText()
    {
        // Arrange
        var plainText = "Consistent encryption test";

        // Act
        var encrypted1 = await _encryptionService.EncryptAsync(plainText);
        var encrypted2 = await _encryptionService.EncryptAsync(plainText);

        // Assert
        encrypted1.Should().Be(encrypted2);
    }

    [Fact]
    public async Task EncryptAsync_WithDifferentPlainText_ShouldReturnDifferentEncryptedText()
    {
        // Arrange
        var plainText1 = "First message";
        var plainText2 = "Second message";

        // Act
        var encrypted1 = await _encryptionService.EncryptAsync(plainText1);
        var encrypted2 = await _encryptionService.EncryptAsync(plainText2);

        // Assert
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void GenerateKey_ShouldReturnValidBase64Key()
    {
        // Act
        var result = _encryptionService.GenerateKey();

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().MatchRegex("^[A-Za-z0-9+/]*={0,3}$");

        // Should be able to decode from base64
        var keyBytes = Convert.FromBase64String(result);
        keyBytes.Length.Should().Be(32); // 256-bit key = 32 bytes
    }

    [Fact]
    public void GenerateKey_CalledMultipleTimes_ShouldReturnDifferentKeys()
    {
        // Act
        var key1 = _encryptionService.GenerateKey();
        var key2 = _encryptionService.GenerateKey();

        // Assert
        key1.Should().NotBe(key2);
    }

    [Fact]
    public async Task ValidateEncryptedDataAsync_WithNullData_ShouldReturnFalse()
    {
        // Act
        var result = await _encryptionService.ValidateEncryptedDataAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEncryptedDataAsync_WithEmptyData_ShouldReturnFalse()
    {
        // Act
        var result = await _encryptionService.ValidateEncryptedDataAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEncryptedDataAsync_WithInvalidData_ShouldReturnFalse()
    {
        // Act
        var result = await _encryptionService.ValidateEncryptedDataAsync("invalid-encrypted-data");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateEncryptedDataAsync_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var plainText = "Test data for validation";
        var encrypted = await _encryptionService.EncryptAsync(plainText);

        // Act
        var result = await _encryptionService.ValidateEncryptedDataAsync(encrypted);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EncryptDecrypt_WithSpecialCharacters_ShouldWork()
    {
        // Arrange
        var plainText = "Special chars: !@#$%^&*()_+{}|:<>?[];',./`~";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptDecrypt_WithUnicodeCharacters_ShouldWork()
    {
        // Arrange
        var plainText = "Unicode: 你好世界 🌍 عالم مرحبا";

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task EncryptDecrypt_WithLargeText_ShouldWork()
    {
        // Arrange
        var plainText = string.Join("", Enumerable.Repeat("This is a long text message. ", 1000));

        // Act
        var encrypted = await _encryptionService.EncryptAsync(plainText);
        var decrypted = await _encryptionService.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Dispose_ShouldNotThrow()
    {
        // Act & Assert
        _encryptionService.Invoking(s => s.Dispose()).Should().NotThrow();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ShouldNotThrow()
    {
        // Act & Assert
        _encryptionService.Dispose();
        _encryptionService.Invoking(s => s.Dispose()).Should().NotThrow();
    }

    public void Dispose()
    {
        _encryptionService?.Dispose();
    }
}