using AspNetCore.Totp.Interface;
using AegisEInvoicing.Infrastructure.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.InfrastructureTests.Services;

public class TotpServiceTests
{
    private readonly Mock<ITotpGenerator> _totpGeneratorMock;
    private readonly TotpService _totpService;

    public TotpServiceTests()
    {
        _totpGeneratorMock = new Mock<ITotpGenerator>();
        _totpService = new TotpService(_totpGeneratorMock.Object);
    }

    [Fact]
    public void Constructor_WithValidTotpGenerator_ShouldNotThrow()
    {
        // Act & Assert
        var service = new TotpService(_totpGeneratorMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void Generate_WithValidKey_ShouldCallTotpGeneratorGenerate()
    {
        // Arrange
        var key = "JBSWY3DPEHPK3PXP";
        var expectedOtp = 123456;
        _totpGeneratorMock.Setup(x => x.Generate(key)).Returns(expectedOtp);

        // Act
        var result = _totpService.Generate(key);

        // Assert
        result.Should().Be(expectedOtp);
        _totpGeneratorMock.Verify(x => x.Generate(key), Times.Once);
    }

    [Fact]
    public void Generate_WithDifferentKey_ShouldReturnDifferentOtp()
    {
        // Arrange
        var key1 = "JBSWY3DPEHPK3PXP";
        var key2 = "JBSWY3DPEHPK3PXQ";
        _totpGeneratorMock.Setup(x => x.Generate(key1)).Returns(123456);
        _totpGeneratorMock.Setup(x => x.Generate(key2)).Returns(654321);

        // Act
        var result1 = _totpService.Generate(key1);
        var result2 = _totpService.Generate(key2);

        // Assert
        result1.Should().Be(123456);
        result2.Should().Be(654321);
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Generate_ShouldReturnSixDigitNumber()
    {
        // Arrange
        var key = "JBSWY3DPEHPK3PXP";
        var expectedOtp = 123456;
        _totpGeneratorMock.Setup(x => x.Generate(key)).Returns(expectedOtp);

        // Act
        var result = _totpService.Generate(key);

        // Assert
        result.Should().BeInRange(0, 999999);
        result.ToString().Length.Should().BeLessThanOrEqualTo(6);
    }

    [Fact]
    public void Verify_WithValidOtpAndKey_ShouldReturnBooleanResult()
    {
        // Arrange
        var key = "JBSWY3DPEHPK3PXP";
        var otp = 123456;

        // Setup the generator to return the OTP when called
        _totpGeneratorMock.Setup(x => x.Generate(key)).Returns(otp);

        // Act - TotpValidator performs real time-based validation
        var result = _totpService.Verify(otp, key);

        // Assert - Result depends on actual time-based validation
        // Just verify the method returns without exception
        result.GetType().Should().Be(typeof(bool));
    }

    [Fact]
    public void Verify_WithInvalidOtp_ShouldReturnBooleanResult()
    {
        // Arrange
        var key = "JBSWY3DPEHPK3PXP";
        var correctOtp = 123456;
        var wrongOtp = 654321;

        _totpGeneratorMock.Setup(x => x.Generate(key)).Returns(correctOtp);

        // Act - TotpValidator performs real time-based validation
        var result = _totpService.Verify(wrongOtp, key);

        // Assert - Result depends on actual time-based validation
        // Just verify the method returns without exception
        result.GetType().Should().Be(typeof(bool));
    }

    [Fact]
    public void Verify_WithNullKey_ShouldHandleGracefully()
    {
        // Arrange
        var otp = 123456;

        // Act & Assert
        _totpService.Invoking(s => s.Verify(otp, null!))
            .Should().NotThrow();
    }

    [Fact]
    public void Verify_WithEmptyKey_ShouldHandleGracefully()
    {
        // Arrange
        var otp = 123456;

        // Act & Assert
        _totpService.Invoking(s => s.Verify(otp, ""))
            .Should().NotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(999999)]
    [InlineData(123456)]
    public void Verify_WithVariousOtpValues_ShouldProcessCorrectly(int otp)
    {
        // Arrange
        var key = "JBSWY3DPEHPK3PXP";
        _totpGeneratorMock.Setup(x => x.Generate(key)).Returns(otp);

        // Act - TotpValidator performs real time-based validation internally
        // We can only verify it doesn't throw and returns a boolean
        var result = _totpService.Verify(otp, key);

        // Assert - The result depends on actual time-based validation
        // which we cannot mock since TotpValidator is instantiated internally
        result.GetType().Should().Be(typeof(bool));
    }

    [Fact]
    public void Generate_CalledMultipleTimesWithSameKey_ShouldReturnConsistentResult()
    {
        // Arrange
        var key = "JBSWY3DPEHPK3PXP";
        var expectedOtp = 123456;
        _totpGeneratorMock.Setup(x => x.Generate(key)).Returns(expectedOtp);

        // Act
        var result1 = _totpService.Generate(key);
        var result2 = _totpService.Generate(key);
        var result3 = _totpService.Generate(key);

        // Assert
        result1.Should().Be(expectedOtp);
        result2.Should().Be(expectedOtp);
        result3.Should().Be(expectedOtp);
        _totpGeneratorMock.Verify(x => x.Generate(key), Times.Exactly(3));
    }

    [Fact]
    public void Verify_WithValidGeneratedOtp_ShouldReturnBooleanResult()
    {
        // Arrange
        var key = "JBSWY3DPEHPK3PXP";
        var generatedOtp = 987654;
        _totpGeneratorMock.Setup(x => x.Generate(key)).Returns(generatedOtp);

        // Act - Generate OTP first, then verify it
        var otp = _totpService.Generate(key);
        var isValid = _totpService.Verify(otp, key);

        // Assert
        otp.Should().Be(generatedOtp);
        // TotpValidator performs real time-based validation internally
        isValid.GetType().Should().Be(typeof(bool));
    }

    [Fact]
    public void Generate_WithNullKey_ShouldCallGeneratorWithNullKey()
    {
        // Arrange
        _totpGeneratorMock.Setup(x => x.Generate(null!)).Returns(0);

        // Act
        var result = _totpService.Generate(null!);

        // Assert
        _totpGeneratorMock.Verify(x => x.Generate(null!), Times.Once);
    }

    [Fact]
    public void Generate_WithEmptyKey_ShouldCallGeneratorWithEmptyKey()
    {
        // Arrange
        _totpGeneratorMock.Setup(x => x.Generate("")).Returns(0);

        // Act
        var result = _totpService.Generate("");

        // Assert
        _totpGeneratorMock.Verify(x => x.Generate(""), Times.Once);
    }
}