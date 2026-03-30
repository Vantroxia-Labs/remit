using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.ValueObjects;

/// <summary>
/// Comprehensive tests for Address value object targeting 100% code coverage
/// </summary>
public class AddressTests
{
    #region Constructor Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateAddress()
    {
        // Act
        var address = Address.Create(
            "123 Main Street",
            "Lagos",
            "Lagos State",
            "Nigeria",
            "100001");

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be("123 Main Street");
        address.City.Should().Be("Lagos");
        address.State.Should().Be("Lagos State");
        address.Country.Should().Be("Nigeria");
        address.PostalCode.Should().Be("100001");
    }

    [Fact]
    public void Create_WithNullPostalCode_ShouldCreateAddressWithEmptyPostalCode()
    {
        // Act
        var address = Address.Create(
            "123 Main Street",
            "Lagos",
            "Lagos State",
            "Nigeria",
            null!);

        // Assert
        address.Should().NotBeNull();
        address.PostalCode.Should().Be(string.Empty);
    }

    [Fact]
    public void Create_WithWhitespaceParameters_ShouldTrimValues()
    {
        // Act
        var address = Address.Create(
            "  123 Main Street  ",
            "  Lagos  ",
            "  Lagos State  ",
            "  Nigeria  ",
            "  100001  ");

        // Assert
        address.Street.Should().Be("123 Main Street");
        address.City.Should().Be("Lagos");
        address.State.Should().Be("Lagos State");
        address.Country.Should().Be("Nigeria");
        address.PostalCode.Should().Be("100001");
    }

    [Theory]
    [InlineData("", "Lagos", "Lagos State", "Nigeria")]
    [InlineData("   ", "Lagos", "Lagos State", "Nigeria")]
    [InlineData(null, "Lagos", "Lagos State", "Nigeria")]
    public void Create_WithInvalidStreet_ShouldThrowArgumentException(string? street, string city, string state, string country)
    {
        // Act & Assert
        var action = () => Address.Create(street!, city!, state!, country!, "100001");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Street cannot be null or empty*");
    }

    [Theory]
    [InlineData("123 Main St", "", "Lagos State", "Nigeria")]
    [InlineData("123 Main St", "   ", "Lagos State", "Nigeria")]
    [InlineData("123 Main St", null, "Lagos State", "Nigeria")]
    public void Create_WithInvalidCity_ShouldThrowArgumentException(string street, string? city, string state, string country)
    {
        // Act & Assert
        var action = () => Address.Create(street!, city!, state!, country!, "100001");
        action.Should().Throw<ArgumentException>()
            .WithMessage("City cannot be null or empty*");
    }

    [Theory]
    [InlineData("123 Main St", "Lagos", "", "Nigeria")]
    [InlineData("123 Main St", "Lagos", "   ", "Nigeria")]
    [InlineData("123 Main St", "Lagos", null, "Nigeria")]
    public void Create_WithInvalidState_ShouldThrowArgumentException(string street, string city, string? state, string country)
    {
        // Act & Assert
        var action = () => Address.Create(street!, city!, state!, country!, "100001");
        action.Should().Throw<ArgumentException>()
            .WithMessage("State cannot be null or empty*");
    }

    [Theory]
    [InlineData("123 Main St", "Lagos", "Lagos State", "")]
    [InlineData("123 Main St", "Lagos", "Lagos State", "   ")]
    [InlineData("123 Main St", "Lagos", "Lagos State", null)]
    public void Create_WithInvalidCountry_ShouldThrowArgumentException(string street, string city, string state, string? country)
    {
        // Act & Assert
        var action = () => Address.Create(street!, city!, state!, country!, "100001");
        action.Should().Throw<ArgumentException>()
            .WithMessage("Country cannot be null or empty*");
    }

    #endregion

    #region Formatting Tests

    [Fact]
    public void GetFormattedAddress_WithAllFields_ShouldReturnFormattedString()
    {
        // Arrange
        var address = Address.Create(
            "123 Main Street",
            "Lagos",
            "Lagos State",
            "Nigeria",
            "100001");

        // Act
        var formatted = address.GetFormattedAddress();

        // Assert
        formatted.Should().Be("123 Main Street, Lagos, Lagos State, 100001, Nigeria");
    }

    [Fact]
    public void GetFormattedAddress_WithoutPostalCode_ShouldReturnFormattedStringWithoutPostalCode()
    {
        // Arrange
        var address = Address.Create(
            "123 Main Street",
            "Lagos",
            "Lagos State",
            "Nigeria",
            null!);

        // Act
        var formatted = address.GetFormattedAddress();

        // Assert
        formatted.Should().Be("123 Main Street, Lagos, Lagos State, Nigeria");
    }

    [Fact]
    public void GetFormattedAddress_WithEmptyPostalCode_ShouldReturnFormattedStringWithoutPostalCode()
    {
        // Arrange
        var address = Address.Create(
            "123 Main Street",
            "Lagos",
            "Lagos State",
            "Nigeria",
            "");

        // Act
        var formatted = address.GetFormattedAddress();

        // Assert
        formatted.Should().Be("123 Main Street, Lagos, Lagos State, Nigeria");
    }

    [Fact]
    public void GetFormattedAddress_WithWhitespacePostalCode_ShouldReturnFormattedStringWithoutPostalCode()
    {
        // Arrange
        var address = Address.Create(
            "123 Main Street",
            "Lagos",
            "Lagos State",
            "Nigeria",
            "   ");

        // Act
        var formatted = address.GetFormattedAddress();

        // Assert
        formatted.Should().Be("123 Main Street, Lagos, Lagos State, Nigeria");
    }

    [Fact]
    public void ToString_ShouldReturnFormattedAddress()
    {
        // Arrange
        var address = Address.Create(
            "123 Main Street",
            "Lagos",
            "Lagos State",
            "Nigeria",
            "100001");

        // Act
        var result = address.ToString();

        // Assert
        result.Should().Be(address.GetFormattedAddress());
        result.Should().Be("123 Main Street, Lagos, Lagos State, 100001, Nigeria");
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameAddressValues_ShouldBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");

        // Act & Assert
        address1.Should().Be(address2);
        address1.Equals(address2).Should().BeTrue();
        (address1 == address2).Should().BeTrue();
        (address1 != address2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentStreet_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("456 Second St", "Lagos", "Lagos State", "Nigeria", "100001");

        // Act & Assert
        address1.Should().NotBe(address2);
        address1.Equals(address2).Should().BeFalse();
        (address1 == address2).Should().BeFalse();
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentCity_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("123 Main St", "Abuja", "Lagos State", "Nigeria", "100001");

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentState_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("123 Main St", "Lagos", "Ogun State", "Nigeria", "100001");

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentCountry_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("123 Main St", "Lagos", "Lagos State", "Ghana", "100001");

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithDifferentPostalCode_ShouldNotBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100002");

        // Act & Assert
        address1.Should().NotBe(address2);
    }

    [Fact]
    public void Equals_WithNullPostalCodeAndEmptyPostalCode_ShouldBeEqual()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "");
        var address2 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "");

        // Act & Assert
        address1.Should().Be(address2);
    }

    [Fact]
    public void Equals_WithNull_ShouldNotBeEqual()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");

        // Act & Assert
        address.Equals(null).Should().BeFalse();
        (address == null).Should().BeFalse();
        (address != null).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldNotBeEqual()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var differentObject = "123 Main St, Lagos, Lagos State, Nigeria";

        // Act & Assert
        address.Equals(differentObject).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameAddressValues_ShouldHaveSameHashCode()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");

        // Act & Assert
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentAddressValues_ShouldHaveDifferentHashCodes()
    {
        // Arrange
        var address1 = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");
        var address2 = Address.Create("456 Second St", "Lagos", "Lagos State", "Nigeria", "100001");

        // Act & Assert
        address1.GetHashCode().Should().NotBe(address2.GetHashCode());
    }

    #endregion

    #region Equality Components Tests

    [Fact]
    public void GetEqualityComponents_ShouldYieldAllAddressFields()
    {
        // Arrange
        var address = Address.Create("123 Main St", "Lagos", "Lagos State", "Nigeria", "100001");

        // Act
        var components = address.GetType()
            .GetMethod("GetEqualityComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(address, null) as IEnumerable<object>;

        // Assert
        components.Should().NotBeNull();
        var componentsList = components!.ToList();
        componentsList.Should().HaveCount(5);
        componentsList[0].Should().Be("123 Main St");
        componentsList[1].Should().Be("Lagos");
        componentsList[2].Should().Be("Lagos State");
        componentsList[3].Should().Be("Nigeria");
        componentsList[4].Should().Be("100001");
    }

    [Fact]
    public void ParameterlessConstructor_ShouldCreateAddressWithEmptyValues()
    {
        // Arrange & Act
        var address = (Address)Activator.CreateInstance(typeof(Address), true)!;

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be(string.Empty);
        address.City.Should().Be(string.Empty);
        address.State.Should().Be(string.Empty);
        address.Country.Should().Be(string.Empty);
        address.PostalCode.Should().Be(string.Empty);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithVeryLongValues_ShouldCreateAddress()
    {
        // Arrange
        var longString = new string('A', 1000);

        // Act
        var address = Address.Create(longString, longString, longString, longString, longString);

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be(longString);
        address.City.Should().Be(longString);
        address.State.Should().Be(longString);
        address.Country.Should().Be(longString);
        address.PostalCode.Should().Be(longString);
    }

    [Fact]
    public void Create_WithSpecialCharacters_ShouldCreateAddress()
    {
        // Act
        var address = Address.Create(
            "123 Main St. Apt #4B",
            "São Paulo",
            "São Paulo State",
            "Brasil",
            "01234-567");

        // Assert
        address.Should().NotBeNull();
        address.Street.Should().Be("123 Main St. Apt #4B");
        address.City.Should().Be("São Paulo");
        address.State.Should().Be("São Paulo State");
        address.Country.Should().Be("Brasil");
        address.PostalCode.Should().Be("01234-567");
    }

    #endregion
}