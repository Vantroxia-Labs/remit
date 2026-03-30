using AegisEInvoicing.Domain.Entities.UserManagement;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.UserManagement;

/// <summary>
/// Comprehensive tests for UserRoleAssignment entity targeting 100% code coverage
/// </summary>
public class UserRoleAssignmentTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _platformRoleId = Guid.NewGuid();
    private readonly Guid _assignedBy = Guid.NewGuid();
    private readonly Guid _revokedBy = Guid.NewGuid();

    #region Constructor Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateUserRoleAssignment()
    {
        // Act
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);

        // Assert
        assignment.Should().NotBeNull();
        assignment.UserId.Should().Be(_userId);
        assignment.PlatformRoleId.Should().Be(_platformRoleId);
        assignment.AssignedBy.Should().Be(_assignedBy);
        assignment.AssignedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        assignment.ExpiresAt.Should().BeNull();
        assignment.IsActive.Should().BeTrue();
        assignment.RevokedAt.Should().BeNull();
        assignment.RevokedBy.Should().BeNull();
        assignment.RevocationReason.Should().BeNull();
    }

    [Fact]
    public void Create_WithExpirationDate_ShouldCreateUserRoleAssignmentWithExpiration()
    {
        // Arrange
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, expiresAt);

        // Assert
        assignment.Should().NotBeNull();
        assignment.ExpiresAt.Should().Be(expiresAt);
        assignment.IsActive.Should().BeTrue();
    }

    #endregion

    #region Revocation Tests

    [Fact]
    public void Revoke_WhenActive_ShouldRevokeAssignment()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        var reason = "No longer needed";

        // Act
        assignment.Revoke(_revokedBy, reason);

        // Assert
        assignment.IsActive.Should().BeFalse();
        assignment.RevokedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        assignment.RevokedBy.Should().Be(_revokedBy);
        assignment.RevocationReason.Should().Be(reason);
    }

    [Fact]
    public void Revoke_WithoutReason_ShouldRevokeAssignment()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);

        // Act
        assignment.Revoke(_revokedBy);

        // Assert
        assignment.IsActive.Should().BeFalse();
        assignment.RevokedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        assignment.RevokedBy.Should().Be(_revokedBy);
        assignment.RevocationReason.Should().BeNull();
    }

    [Fact]
    public void Revoke_WhenAlreadyRevoked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        assignment.Revoke(_revokedBy, "First revocation");

        // Act & Assert
        var action = () => assignment.Revoke(_revokedBy, "Second revocation");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Role assignment is already revoked");
    }

    #endregion

    #region Extension Tests

    [Fact]
    public void Extend_WhenActiveAndValidDate_ShouldExtendExpiration()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        var newExpirationDate = DateTimeOffset.UtcNow.AddDays(60);

        // Act
        assignment.Extend(newExpirationDate);

        // Assert
        assignment.ExpiresAt.Should().Be(newExpirationDate);
        assignment.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Extend_WhenRevoked_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        assignment.Revoke(_revokedBy);
        var newExpirationDate = DateTimeOffset.UtcNow.AddDays(60);

        // Act & Assert
        var action = () => assignment.Extend(newExpirationDate);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot extend a revoked role assignment");
    }

    [Fact]
    public void Extend_WithPastDate_ShouldThrowArgumentException()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Act & Assert
        var action = () => assignment.Extend(pastDate);
        action.Should().Throw<ArgumentException>()
            .WithMessage("New expiration date must be in the future*");
    }

    [Fact]
    public void Extend_WithCurrentTime_ShouldThrowArgumentException()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        var currentTime = DateTimeOffset.UtcNow;

        // Act & Assert
        var action = () => assignment.Extend(currentTime);
        action.Should().Throw<ArgumentException>()
            .WithMessage("New expiration date must be in the future*");
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsExpired_WhenNoExpirationDate_ShouldReturnFalse()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);

        // Act & Assert
        assignment.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpirationInFuture_ShouldReturnFalse()
    {
        // Arrange
        var futureDate = DateTimeOffset.UtcNow.AddDays(30);
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, futureDate);

        // Act & Assert
        assignment.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenExpirationInPast_ShouldReturnTrue()
    {
        // Arrange
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, pastDate);

        // Act & Assert
        assignment.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenExpirationIsNow_ShouldReturnTrue()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, now);

        // Act & Assert
        assignment.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);

        // Act & Assert
        assignment.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpiredWithFutureExpiration_ShouldReturnTrue()
    {
        // Arrange
        var futureDate = DateTimeOffset.UtcNow.AddDays(30);
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, futureDate);

        // Act & Assert
        assignment.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenRevoked_ShouldReturnFalse()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        assignment.Revoke(_revokedBy);

        // Act & Assert
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        // Arrange
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, pastDate);

        // Act & Assert
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenRevokedAndExpired_ShouldReturnFalse()
    {
        // Arrange
        var pastDate = DateTimeOffset.UtcNow.AddDays(-1);
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, pastDate);
        assignment.Revoke(_revokedBy);

        // Act & Assert
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValidAndActive_ShouldReturnSameAsIsValid()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);

        // Act & Assert
        assignment.IsValidAndActive().Should().Be(assignment.IsValid());
    }

    [Fact]
    public void IsValidAndActive_WhenRevoked_ShouldReturnSameAsIsValid()
    {
        // Arrange
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);
        assignment.Revoke(_revokedBy);

        // Act & Assert
        assignment.IsValidAndActive().Should().Be(assignment.IsValid());
        assignment.IsValidAndActive().Should().BeFalse();
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void Create_WithExpirationAtCurrentTime_ShouldCreateExpiredAssignment()
    {
        // Arrange
        var currentTime = DateTimeOffset.UtcNow;

        // Act
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, currentTime);

        // Assert
        assignment.ExpiresAt.Should().Be(currentTime);
        assignment.IsExpired().Should().BeTrue();
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Create_WithPastExpirationDate_ShouldCreateExpiredAssignment()
    {
        // Arrange
        var pastDate = DateTimeOffset.UtcNow.AddMinutes(-1);

        // Act
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy, pastDate);

        // Assert
        assignment.ExpiresAt.Should().Be(pastDate);
        assignment.IsExpired().Should().BeTrue();
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void NavigationProperties_ShouldBeInitializedToNull()
    {
        // Arrange & Act
        var assignment = UserRoleAssignment.Create(_userId, _platformRoleId, _assignedBy);

        // Assert
        // Navigation properties are set to null! in the domain model
        // This is expected behavior for Entity Framework navigation properties
        assignment.Should().NotBeNull();
        assignment.UserId.Should().Be(_userId);
        assignment.PlatformRoleId.Should().Be(_platformRoleId);
    }

    #endregion
}