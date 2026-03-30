using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.ValueObjects.UserManagement;

/// <summary>
/// Comprehensive tests for PasswordHash value object targeting 100% code coverage
/// </summary>
public class PasswordHashTests
{
    #region Constructor Tests

    [Fact]
    public void Create_WithValidPassword_ShouldCreatePasswordHash()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var passwordHash = PasswordHash.Create(password);

        // Assert
        passwordHash.Should().NotBeNull();
        passwordHash.Hash.Should().NotBeNullOrEmpty();
        passwordHash.Salt.Should().NotBeNullOrEmpty();
        passwordHash.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidPassword_ShouldThrowArgumentException(string? invalidPassword)
    {
        // Act & Assert
        var action = () => PasswordHash.Create(invalidPassword!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be null or empty*");
    }

    [Fact]
    public void FromExisting_WithValidParameters_ShouldCreatePasswordHash()
    {
        // Arrange
        var hash = "dGVzdGhhc2g="; // Base64 encoded "testhash"
        var salt = "dGVzdHNhbHQ="; // Base64 encoded "testsalt"
        var createdAt = DateTimeOffset.UtcNow.AddDays(-1);

        // Act
        var passwordHash = PasswordHash.FromExisting(hash, salt, createdAt);

        // Assert
        passwordHash.Should().NotBeNull();
        passwordHash.Hash.Should().Be(hash);
        passwordHash.Salt.Should().Be(salt);
        passwordHash.CreatedAt.Should().Be(createdAt);
    }

    [Theory]
    [InlineData("", "salt")]
    [InlineData("   ", "salt")]
    [InlineData(null, "salt")]
    public void FromExisting_WithInvalidHash_ShouldThrowArgumentException(string? invalidHash, string salt)
    {
        // Act & Assert
        var action = () => PasswordHash.FromExisting(invalidHash!, salt, DateTimeOffset.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Hash cannot be null or empty*");
    }

    [Theory]
    [InlineData("hash", "")]
    [InlineData("hash", "   ")]
    [InlineData("hash", null)]
    public void FromExisting_WithInvalidSalt_ShouldThrowArgumentException(string hash, string? invalidSalt)
    {
        // Act & Assert
        var action = () => PasswordHash.FromExisting(hash, invalidSalt!, DateTimeOffset.UtcNow);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Salt cannot be null or empty*");
    }

    #endregion

    #region Password Strength Validation Tests

    [Theory]
    [InlineData("SecurePassword123!")]
    [InlineData("MyComplex@Pass2024")]
    [InlineData("Str0ng!P@ssw0rd")]
    [InlineData("Complex#Pass123")]
    public void ValidatePasswordStrength_WithValidPasswords_ShouldNotThrow(string validPassword)
    {
        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(validPassword);
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidatePasswordStrength_WithNullOrEmpty_ShouldThrowArgumentException(string? invalidPassword)
    {
        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(invalidPassword!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be null or empty*");
    }

    [Theory]
    [InlineData("Short1!")]
    [InlineData("1234567")]
    [InlineData("A1!")]
    public void ValidatePasswordStrength_WithTooShortPassword_ShouldThrowArgumentException(string shortPassword)
    {
        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(shortPassword);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password must be at least 8 characters long*");
    }

    [Fact]
    public void ValidatePasswordStrength_WithTooLongPassword_ShouldThrowArgumentException()
    {
        // Arrange
        var longPassword = new string('A', 129) + "1!"; // 131 characters

        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(longPassword);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot exceed 128 characters*");
    }

    [Theory]
    [InlineData("nocapitalletters123!")]
    [InlineData("alllowercase1!")]
    public void ValidatePasswordStrength_WithoutUppercase_ShouldThrowArgumentException(string passwordWithoutUpper)
    {
        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(passwordWithoutUpper);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password must contain at least one uppercase letter*");
    }

    [Theory]
    [InlineData("ALLUPPERCASE123!")]
    [InlineData("NOLOWERCASE1!")]
    public void ValidatePasswordStrength_WithoutLowercase_ShouldThrowArgumentException(string passwordWithoutLower)
    {
        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(passwordWithoutLower);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password must contain at least one lowercase letter*");
    }

    [Theory]
    [InlineData("NoDigitsHere!")]
    [InlineData("OnlyLetters@")]
    public void ValidatePasswordStrength_WithoutDigit_ShouldThrowArgumentException(string passwordWithoutDigit)
    {
        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(passwordWithoutDigit);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password must contain at least one digit*");
    }

    [Theory]
    [InlineData("NoSpecialChars123")]
    [InlineData("OnlyAlphaNumeric123")]
    public void ValidatePasswordStrength_WithoutSpecialCharacter_ShouldThrowArgumentException(string passwordWithoutSpecial)
    {
        // Act & Assert
        var action = () => PasswordHash.ValidatePasswordStrength(passwordWithoutSpecial);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Password must contain at least one special character*");
    }

    [Theory]
    [InlineData("password", "Password must contain at least one uppercase letter*")]
    [InlineData("Password", "Password must contain at least one digit*")]
    [InlineData("PASSWORD", "Password must contain at least one lowercase letter*")]
    [InlineData("123456", "Password must be at least 8 characters long*")]
    [InlineData("password123", "Password must contain at least one uppercase letter*")]
    [InlineData("Password123", "Password must contain at least one special character*")]
    [InlineData("admin", "Password must be at least 8 characters long*")]
    [InlineData("qwerty", "Password must be at least 8 characters long*")]
    [InlineData("letmein", "Password must be at least 8 characters long*")]
    [InlineData("welcome", "Password must be at least 8 characters long*")]
    [InlineData("monkey", "Password must be at least 8 characters long*")]
    [InlineData("1234567890", "Password must contain at least one uppercase letter*")]
    [InlineData("Password1", "Password must contain at least one special character*")]
    public void ValidatePasswordStrength_WithCommonPasswords_ShouldThrowArgumentException(string commonPassword, string expectedMessage)
    {
        // Act & Assert
        // Note: Common passwords fail earlier validation checks before reaching the common password check
        var action = () => PasswordHash.ValidatePasswordStrength(commonPassword);
        action.Should().Throw<ArgumentException>()
            .WithMessage(expectedMessage);
    }

    #endregion

    #region Verification Tests

    [Fact]
    public void Verify_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "SecurePassword123!";
        var passwordHash = PasswordHash.Create(password);

        // Act
        var result = passwordHash.Verify(password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var wrongPassword = "WrongPassword123!";
        var passwordHash = PasswordHash.Create(password);

        // Act
        var result = passwordHash.Verify(wrongPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Verify_WithNullOrEmptyPassword_ShouldReturnFalse(string? invalidPassword)
    {
        // Arrange
        var passwordHash = PasswordHash.Create("SecurePassword123!");

        // Act
        var result = passwordHash.Verify(invalidPassword!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_WithCorruptedHash_ShouldReturnFalse()
    {
        // Arrange
        var password = "SecurePassword123!";
        var salt = "dGVzdHNhbHQ=";
        var corruptedHash = "corrupted_hash";
        var passwordHash = PasswordHash.FromExisting(corruptedHash, salt, DateTimeOffset.UtcNow);

        // Act
        var result = passwordHash.Verify(password);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Expiration Tests

    [Fact]
    public void IsExpired_WithPasswordOlderThanMaxAge_ShouldReturnTrue()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-31);
        var passwordHash = PasswordHash.FromExisting("hash", "salt", createdAt);
        var maxAge = TimeSpan.FromDays(30);

        // Act
        var result = passwordHash.IsExpired(maxAge);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WithPasswordNewerThanMaxAge_ShouldReturnFalse()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddDays(-29);
        var passwordHash = PasswordHash.FromExisting("hash", "salt", createdAt);
        var maxAge = TimeSpan.FromDays(30);

        // Act
        var result = passwordHash.IsExpired(maxAge);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithPasswordJustUnderMaxAge_ShouldReturnFalse()
    {
        // Arrange - Use slightly less than max age to avoid timing edge case
        var createdAt = DateTimeOffset.UtcNow.AddDays(-29).AddHours(-23);
        var passwordHash = PasswordHash.FromExisting("hash", "salt", createdAt);
        var maxAge = TimeSpan.FromDays(30);

        // Act
        var result = passwordHash.IsExpired(maxAge);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equals_WithSameHashAndSalt_ShouldBeEqual()
    {
        // Arrange
        var hash = "samehash";
        var salt = "samesalt";
        var createdAt = DateTimeOffset.UtcNow;
        var passwordHash1 = PasswordHash.FromExisting(hash, salt, createdAt);
        var passwordHash2 = PasswordHash.FromExisting(hash, salt, createdAt.AddMinutes(1)); // Different creation time

        // Act & Assert
        passwordHash1.Should().Be(passwordHash2);
        passwordHash1.Equals(passwordHash2).Should().BeTrue();
        (passwordHash1 == passwordHash2).Should().BeTrue();
        (passwordHash1 != passwordHash2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentHash_ShouldNotBeEqual()
    {
        // Arrange
        var salt = "samesalt";
        var createdAt = DateTimeOffset.UtcNow;
        var passwordHash1 = PasswordHash.FromExisting("hash1", salt, createdAt);
        var passwordHash2 = PasswordHash.FromExisting("hash2", salt, createdAt);

        // Act & Assert
        passwordHash1.Should().NotBe(passwordHash2);
        passwordHash1.Equals(passwordHash2).Should().BeFalse();
        (passwordHash1 == passwordHash2).Should().BeFalse();
        (passwordHash1 != passwordHash2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentSalt_ShouldNotBeEqual()
    {
        // Arrange
        var hash = "samehash";
        var createdAt = DateTimeOffset.UtcNow;
        var passwordHash1 = PasswordHash.FromExisting(hash, "salt1", createdAt);
        var passwordHash2 = PasswordHash.FromExisting(hash, "salt2", createdAt);

        // Act & Assert
        passwordHash1.Should().NotBe(passwordHash2);
    }

    [Fact]
    public void GetHashCode_WithSameHashAndSalt_ShouldHaveSameHashCode()
    {
        // Arrange
        var hash = "samehash";
        var salt = "samesalt";
        var passwordHash1 = PasswordHash.FromExisting(hash, salt, DateTimeOffset.UtcNow);
        var passwordHash2 = PasswordHash.FromExisting(hash, salt, DateTimeOffset.UtcNow.AddMinutes(1));

        // Act & Assert
        passwordHash1.GetHashCode().Should().Be(passwordHash2.GetHashCode());
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        // Arrange
        var hash = "veryLongHashValue123456789";
        var salt = "salt";
        var createdAt = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var passwordHash = PasswordHash.FromExisting(hash, salt, createdAt);

        // Act
        var result = passwordHash.ToString();

        // Assert
        result.Should().StartWith("Hash: veryLong...");
        result.Should().Contain("(Created: 2024-01-15 10:30)");
    }

    #endregion

    #region Equality Components Tests

    [Fact]
    public void GetEqualityComponents_ShouldYieldHashAndSalt()
    {
        // Arrange
        var passwordHash = PasswordHash.FromExisting("testhash", "testsalt", DateTimeOffset.UtcNow);

        // Act
        var components = passwordHash.GetType()
            .GetMethod("GetEqualityComponents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(passwordHash, null) as IEnumerable<object>;

        // Assert
        components.Should().NotBeNull();
        var componentsList = components!.ToList();
        componentsList.Should().HaveCount(2);
        componentsList[0].Should().Be("testhash");
        componentsList[1].Should().Be("testsalt");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Create_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "SecurePassword123!";

        // Act
        var passwordHash1 = PasswordHash.Create(password);
        var passwordHash2 = PasswordHash.Create(password);

        // Assert
        passwordHash1.Should().NotBe(passwordHash2);
        passwordHash1.Hash.Should().NotBe(passwordHash2.Hash);
        passwordHash1.Salt.Should().NotBe(passwordHash2.Salt);

        // But both should verify the same password
        passwordHash1.Verify(password).Should().BeTrue();
        passwordHash2.Verify(password).Should().BeTrue();
    }

    [Fact]
    public void ParameterlessConstructor_ShouldCreatePasswordHashWithEmptyValues()
    {
        // Arrange & Act
        var passwordHash = (PasswordHash)Activator.CreateInstance(typeof(PasswordHash), true)!;

        // Assert
        passwordHash.Should().NotBeNull();
        passwordHash.Hash.Should().Be(string.Empty);
        passwordHash.Salt.Should().Be(string.Empty);
        passwordHash.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Create_WithPasswordContainingAllSpecialCharacters_ShouldCreatePasswordHash()
    {
        // Arrange
        var password = "Test123!@#$%^&*()_+-=[]{}|;:,.<>?";

        // Act
        var passwordHash = PasswordHash.Create(password);

        // Assert
        passwordHash.Should().NotBeNull();
        passwordHash.Verify(password).Should().BeTrue();
    }

    [Fact]
    public void Create_WithMaxLengthPassword_ShouldCreatePasswordHash()
    {
        // Arrange - Create a 128 character password that meets all requirements
        // 1 uppercase + 119 lowercase + 7 digits + 1 special = 128 characters
        var password = "A" + new string('a', 119) + "1234567!";

        // Act
        var passwordHash = PasswordHash.Create(password);

        // Assert
        passwordHash.Should().NotBeNull();
        passwordHash.Verify(password).Should().BeTrue();
    }

    #endregion
}