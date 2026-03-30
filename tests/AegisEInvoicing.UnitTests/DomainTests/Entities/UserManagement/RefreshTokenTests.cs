using AegisEInvoicing.Domain.Entities.UserManagement;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.UserManagement;

public class RefreshTokenTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _validToken = "valid_refresh_token_123456";
    private readonly string _validIpAddress = "192.168.1.100";

    [Fact]
    public void Create_WithValidParameters_ShouldCreateRefreshToken()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        var refreshToken = RefreshToken.Create(_userId, _validToken, expiresAt, _validIpAddress);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.Id.Should().NotBeEmpty();
        refreshToken.UserId.Should().Be(_userId);
        refreshToken.Token.Should().Be(_validToken);
        refreshToken.ExpiresAt.Should().Be(expiresAt);
        refreshToken.CreatedByIp.Should().Be(_validIpAddress);
        refreshToken.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        refreshToken.RevokedAt.Should().BeNull();
        refreshToken.RevokedByIp.Should().BeNull();
        refreshToken.RevokedReason.Should().BeNull();
        refreshToken.ReplacedByToken.Should().BeNull();
        refreshToken.IsActive.Should().BeTrue();
        refreshToken.IsExpired.Should().BeFalse();
        refreshToken.IsRevoked.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidToken_ShouldThrowArgumentException(string? invalidToken)
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act & Assert
        var action = () => RefreshToken.Create(_userId, invalidToken!, expiresAt, _validIpAddress);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Token is required*")
            .And.ParamName.Should().Be("token");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidIpAddress_ShouldThrowArgumentException(string? invalidIpAddress)
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);

        // Act & Assert
        var action = () => RefreshToken.Create(_userId, _validToken, expiresAt, invalidIpAddress!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("IP address is required*")
            .And.ParamName.Should().Be("createdByIp");
    }

    [Fact]
    public void Create_WithPastExpirationDate_ShouldThrowArgumentException()
    {
        // Arrange
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Act & Assert
        var action = () => RefreshToken.Create(_userId, _validToken, pastDate, _validIpAddress);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Expiration date must be in the future*")
            .And.ParamName.Should().Be("expiresAt");
    }

    [Fact]
    public void Create_WithCurrentTime_ShouldThrowArgumentException()
    {
        // Arrange
        var currentTime = DateTimeOffset.UtcNow;

        // Act & Assert
        var action = () => RefreshToken.Create(_userId, _validToken, currentTime, _validIpAddress);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Expiration date must be in the future*");
    }

    [Fact]
    public void Revoke_WithValidParameters_ShouldRevokeToken()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        var revokedByIp = "192.168.1.200";
        var reason = "User logged out";
        var replacedByToken = "new_token_456";

        // Act
        refreshToken.Revoke(revokedByIp, reason, replacedByToken);

        // Assert
        refreshToken.RevokedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        refreshToken.RevokedByIp.Should().Be(revokedByIp);
        refreshToken.RevokedReason.Should().Be(reason);
        refreshToken.ReplacedByToken.Should().Be(replacedByToken);
        refreshToken.IsRevoked.Should().BeTrue();
        refreshToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WithoutReplacementToken_ShouldRevokeToken()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        var revokedByIp = "192.168.1.200";
        var reason = "Security concern";

        // Act
        refreshToken.Revoke(revokedByIp, reason);

        // Assert
        refreshToken.RevokedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        refreshToken.RevokedByIp.Should().Be(revokedByIp);
        refreshToken.RevokedReason.Should().Be(reason);
        refreshToken.ReplacedByToken.Should().BeNull();
        refreshToken.IsRevoked.Should().BeTrue();
        refreshToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        refreshToken.Revoke("192.168.1.100", "First revocation");

        // Act & Assert
        var action = () => refreshToken.Revoke("192.168.1.200", "Second revocation");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Token is already revoked");
    }

    [Fact]
    public void IsExpired_WithFutureExpirationDate_ShouldReturnFalse()
    {
        // Arrange
        var futureDate = DateTimeOffset.UtcNow.AddDays(7);
        var refreshToken = RefreshToken.Create(_userId, _validToken, futureDate, _validIpAddress);

        // Act & Assert
        refreshToken.IsExpired.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WithPastExpirationDate_ShouldReturnTrue()
    {
        // Arrange - Create token that will expire immediately
        var expiresAt = DateTimeOffset.UtcNow.AddMilliseconds(1);
        var refreshToken = RefreshToken.Create(_userId, _validToken, expiresAt, _validIpAddress);

        // Wait for expiration
        System.Threading.Thread.Sleep(10);

        // Act & Assert
        refreshToken.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_WithNonRevokedToken_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();

        // Act & Assert
        refreshToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_WithRevokedToken_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        refreshToken.Revoke("192.168.1.100", "Revoked for testing");

        // Act & Assert
        refreshToken.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WithValidNonExpiredNonRevokedToken_ShouldReturnTrue()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();

        // Act & Assert
        refreshToken.IsActive.Should().BeTrue();
    }

    [Fact]
    public void IsActive_WithRevokedToken_ShouldReturnFalse()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        refreshToken.Revoke("192.168.1.100", "Test revocation");

        // Act & Assert
        refreshToken.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsActive_WithExpiredToken_ShouldReturnFalse()
    {
        // Arrange - Create token that expires quickly
        var expiresAt = DateTimeOffset.UtcNow.AddMilliseconds(1);
        var refreshToken = RefreshToken.Create(_userId, _validToken, expiresAt, _validIpAddress);

        // Wait for expiration
        System.Threading.Thread.Sleep(10);

        // Act & Assert
        refreshToken.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("192.168.1.1")]
    [InlineData("10.0.0.1")]
    [InlineData("::1")]
    public void Create_WithDifferentIpAddresses_ShouldWork(string ipAddress)
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var refreshToken = RefreshToken.Create(_userId, _validToken, expiresAt, ipAddress);

        // Assert
        refreshToken.CreatedByIp.Should().Be(ipAddress);
    }

    [Fact]
    public void Create_WithLongToken_ShouldWork()
    {
        // Arrange
        var longToken = new string('A', 500); // Very long token
        var expiresAt = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var refreshToken = RefreshToken.Create(_userId, longToken, expiresAt, _validIpAddress);

        // Assert
        refreshToken.Token.Should().Be(longToken);
    }

    [Fact]
    public void Revoke_WithLongReason_ShouldWork()
    {
        // Arrange
        var refreshToken = CreateTestRefreshToken();
        var longReason = new string('X', 500); // Very long reason

        // Act
        refreshToken.Revoke("192.168.1.100", longReason);

        // Assert
        refreshToken.RevokedReason.Should().Be(longReason);
    }

    [Fact]
    public void Create_WithEmptyUserId_ShouldStillCreateToken()
    {
        // Arrange
        var emptyUserId = Guid.Empty;
        var expiresAt = DateTimeOffset.UtcNow.AddDays(1);

        // Act
        var refreshToken = RefreshToken.Create(emptyUserId, _validToken, expiresAt, _validIpAddress);

        // Assert
        refreshToken.UserId.Should().Be(Guid.Empty);
    }

    private RefreshToken CreateTestRefreshToken()
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        return RefreshToken.Create(_userId, _validToken, expiresAt, _validIpAddress);
    }
}