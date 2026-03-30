using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.ValueObjects;

/// <summary>
/// Comprehensive tests for TIN value object targeting 100% code coverage
/// </summary>
public class TINTests
{
    #region Constructor Tests

    [Theory]
    [InlineData("12345678-9012")]
    [InlineData("123456-789012")]
    [InlineData("1234567890-12")]
    public void Create_WithValidNigerianTIN_ShouldCreateTIN(string validTin)
    {
        // Act
        var tin = TIN.Create(validTin);

        // Assert
        tin.Should().NotBeNull();
        tin.Value.Should().Be(validTin);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyValue_ShouldThrowArgumentException(string invalidValue)
    {
        // Act & Assert
        var action = () => TIN.Create(invalidValue);
        action.Should().Throw<ArgumentException>()
            .WithMessage("TIN cannot be null or empty*");
    }

    [Theory]
    [InlineData("1234567890123")] // 13 digits
    [InlineData("12345678901")] // 11 digits
    [InlineData("123456789")] // 9 digits
    [InlineData("123456789abc")] // contains letters
    [InlineData("12345678901a")] // contains letter at end
    [InlineData("a23456789012")] // contains letter at start
    [InlineData("1234567890123456")] // too long
    [InlineData("123")] // too short
    [InlineData("123456789012345")] // no dash, too long
    [InlineData("12345678901234567890")] // way too long
    public void Create_WithInvalidFormat_ShouldThrowArgumentException(string invalidTin)
    {
        // Act & Assert
        var action = () => TIN.Create(invalidTin);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Nigerian TIN format*");
    }

    [Fact]
    public void Create_WithTINWithoutDash_ShouldThrowArgumentException()
    {
        // Arrange
        var tinWithoutDash = "123456789012";

        // Act & Assert
        var action = () => TIN.Create(tinWithoutDash);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Nigerian TIN format*");
    }

    [Theory]
    [InlineData("123-456-789012")] // Multiple dashes
    [InlineData("123456789-012-")] // Dash at end - results in more than 2 parts
    [InlineData("12345--6789012")] // Double dash - results in 3 parts
    public void Create_WithMultipleOrMisplacedDashes_ShouldThrowArgumentException(string invalidTin)
    {
        // Act & Assert
        var action = () => TIN.Create(invalidTin);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Nigerian TIN format*");
    }

    [Fact]
    public void Create_WithDashAtStart_ShouldCreateValidTIN()
    {
        // Arrange - Dash at start is valid if total digits = 12
        var tinWithDashAtStart = "-123456789012";

        // Act
        var tin = TIN.Create(tinWithDashAtStart);

        // Assert - This is valid because after removing dash, it's 12 digits
        tin.Should().NotBeNull();
        tin.Value.Should().Be(tinWithDashAtStart);
    }

    [Theory]
    [InlineData("123456 78-9012")] // Space in first part
    [InlineData("12345678- 9012")] // Space in second part
    [InlineData(" 12345678-9012")] // Leading space
    [InlineData("12345678-9012 ")] // Trailing space
    public void Create_WithSpacesInTIN_ShouldCreateTINIfValidAfterCleaning(string tinWithSpaces)
    {
        // Act
        var tin = TIN.Create(tinWithSpaces);

        // Assert
        tin.Should().NotBeNull();
        tin.Value.Should().Be(tinWithSpaces);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameTINValue_ShouldBeEqual()
    {
        // Arrange
        var tin1 = TIN.Create("12345678-9012");
        var tin2 = TIN.Create("12345678-9012");

        // Act & Assert
        tin1.Should().Be(tin2);
        tin1.Equals(tin2).Should().BeTrue();
        (tin1 == tin2).Should().BeTrue();
        (tin1 != tin2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentTINValues_ShouldNotBeEqual()
    {
        // Arrange
        var tin1 = TIN.Create("12345678-9012");
        var tin2 = TIN.Create("12345678-9013");

        // Act & Assert
        tin1.Should().NotBe(tin2);
        tin1.Equals(tin2).Should().BeFalse();
        (tin1 == tin2).Should().BeFalse();
        (tin1 != tin2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var tin = TIN.Create("12345678-9012");

        // Act & Assert
        tin.Equals(null).Should().BeFalse();
        (tin == null).Should().BeFalse();
        (tin != null).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldNotBeEqual()
    {
        // Arrange
        var tin = TIN.Create("12345678-9012");
        var differentObject = "12345678-9012";

        // Act & Assert
        tin.Equals(differentObject).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameTINValue_ShouldHaveSameHashCode()
    {
        // Arrange
        var tin1 = TIN.Create("12345678-9012");
        var tin2 = TIN.Create("12345678-9012");

        // Act & Assert
        tin1.GetHashCode().Should().Be(tin2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentTINValues_ShouldHaveDifferentHashCodes()
    {
        // Arrange
        var tin1 = TIN.Create("12345678-9012");
        var tin2 = TIN.Create("12345678-9013");

        // Act & Assert
        tin1.GetHashCode().Should().NotBe(tin2.GetHashCode());
    }

    #endregion

    #region Conversion Tests

    [Fact]
    public void ToString_ShouldReturnTINValue()
    {
        // Arrange
        var tinValue = "12345678-9012";
        var tin = TIN.Create(tinValue);

        // Act
        var result = tin.ToString();

        // Assert
        result.Should().Be(tinValue);
    }

    [Fact]
    public void ImplicitConversionToString_ShouldReturnTINValue()
    {
        // Arrange
        var tinValue = "12345678-9012";
        var tin = TIN.Create(tinValue);

        // Act
        string result = tin;

        // Assert
        result.Should().Be(tinValue);
    }

    #endregion

    #region Edge Cases and Validation

    [Fact]
    public void Create_WithExactly12DigitsWithDash_ShouldCreateTIN()
    {
        // Arrange
        var validTin = "123456789012"; // Should fail - no dash
        var validTinWithDash = "12345678-9012"; // Should pass

        // Act & Assert
        var action1 = () => TIN.Create(validTin);
        action1.Should().Throw<ArgumentException>();

        var tin = TIN.Create(validTinWithDash);
        tin.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithDashInDifferentPositions_ShouldHandleCorrectly()
    {
        // Arrange & Act & Assert
        var tin1 = TIN.Create("1-23456789012"); // 1 + 11 digits
        var tin2 = TIN.Create("123456-789012"); // 6 + 6 digits
        var tin3 = TIN.Create("12345678901-2"); // 11 + 1 digits

        tin1.Should().NotBeNull();
        tin2.Should().NotBeNull();
        tin3.Should().NotBeNull();
    }

    [Theory]
    [InlineData("12345678-901")]  // 11 digits total
    [InlineData("123456789-0123")] // 13 digits total
    public void Create_WithIncorrectDigitCount_ShouldThrowArgumentException(string invalidTin)
    {
        // Act & Assert
        var action = () => TIN.Create(invalidTin);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Invalid Nigerian TIN format*");
    }

    [Fact]
    public void Create_WithDifferentValidSplits_ShouldCreateTIN()
    {
        // Arrange - Different valid splits (all have 12 digits total and one dash)
        var validTin = "1234567-89012"; // 7 + 5 digits

        // Act
        var tin = TIN.Create(validTin);

        // Assert
        tin.Should().NotBeNull();
        tin.Value.Should().Be(validTin);
    }

    [Fact]
    public void GetEqualityComponents_ShouldYieldValue()
    {
        // Arrange
        var tin = TIN.Create("12345678-9012");

        // Act
        var components = tin.GetType()
            .GetMethod("GetEqualityComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(tin, null) as IEnumerable<object>;

        // Assert
        components.Should().NotBeNull();
        components.Should().HaveCount(1);
        components.First().Should().Be("12345678-9012");
    }

    [Fact]
    public void ParameterlessConstructor_ShouldCreateTINWithEmptyValue()
    {
        // Arrange & Act
        var tin = (TIN)Activator.CreateInstance(typeof(TIN), true)!;

        // Assert
        tin.Should().NotBeNull();
        tin.Value.Should().Be(string.Empty);
    }

    #endregion
}