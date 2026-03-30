using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Events.UserManagement;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.UserManagement;

/// <summary>
/// Comprehensive tests for User entity targeting 100% code coverage
/// </summary>
public class UserTests
{
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _branchId = Guid.NewGuid();
    private readonly Guid _createdBy = Guid.NewGuid();
    private readonly Guid _updatedBy = Guid.NewGuid();
    private readonly PasswordHash _passwordHash = PasswordHash.Create("SecurePassword123!");

    #region Constructor Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateUser()
    {
        // Act
        var user = User.Create(
            _businessId,
            "John",
            "Doe",
            "john.doe@example.com",
            _passwordHash,
            _createdBy,
            _branchId,
            "+1234567890");

        // Assert
        user.Should().NotBeNull();
        user.BusinessId.Should().Be(_businessId);
        user.BranchId.Should().Be(_branchId);
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.Email.Should().Be("john.doe@example.com");
        user.PhoneNumber.Should().Be("+1234567890");
        user.PasswordHash.Should().Be(_passwordHash);
        user.Status.Should().Be(UserStatus.Active);
        user.IsEmailVerified.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.MustChangePassword.Should().BeTrue();
        user.IsAegisUser.Should().BeFalse();
        user.AegisRole.Should().BeNull();
        user.DomainEvents.Should().HaveCount(1);
        user.DomainEvents.First().Should().BeOfType<UserCreatedEvent>();
    }

    [Fact]
    public void Create_WithoutOptionalParameters_ShouldCreateUser()
    {
        // Act
        var user = User.Create(
            _businessId,
            "John",
            "Doe",
            "john.doe@example.com",
            _passwordHash,
            _createdBy);

        // Assert
        user.Should().NotBeNull();
        user.BranchId.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
        user.Status.Should().Be(UserStatus.Active);
    }

    [Theory]
    [InlineData("", "Doe", "john.doe@example.com")]
    [InlineData("John", "", "john.doe@example.com")]
    [InlineData("John", "Doe", "")]
    [InlineData(null, "Doe", "john.doe@example.com")]
    [InlineData("John", null, "john.doe@example.com")]
    [InlineData("John", "Doe", null)]
    [InlineData("John", "Doe", "invalid-email")]
    public void Create_WithInvalidParameters_ShouldThrowArgumentException(string? firstName, string? lastName, string? email)
    {
        // Act & Assert
        var action = () => User.Create(_businessId, firstName!, lastName!, email!, _passwordHash, _createdBy);
        action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateAegisUser_WithValidParameters_ShouldCreateAegisUser()
    {
        // Act
        var user = User.CreateAegisUser(
            "Jane",
            "Smith",
            "jane.smith@Aegis.com",
            _passwordHash,
            AegisRole.AegisAdmin,
            _createdBy,
            "+1234567890",
            "EMP001",
            "Finance");

        // Assert
        user.Should().NotBeNull();
        user.BusinessId.Should().BeNull();
        user.BranchId.Should().BeNull();
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.Email.Should().Be("jane.smith@Aegis.com");
        user.PhoneNumber.Should().Be("+1234567890");
        user.IsAegisUser.Should().BeTrue();
        user.AegisRole.Should().Be(AegisRole.AegisAdmin);
        user.AegisEmployeeId.Should().Be("EMP001");
        user.AegisDepartment.Should().Be("Finance");
        user.LastAegisActivityAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        user.DomainEvents.Should().HaveCount(2); // UserCreatedEvent and UserAegisRoleChangedEvent
    }

    [Fact]
    public void CreateAegisUser_WithoutOptionalParameters_ShouldCreateAegisUser()
    {
        // Act
        var user = User.CreateAegisUser(
            "Jane",
            "Smith",
            "jane.smith@Aegis.com",
            _passwordHash,
            AegisRole.AegisAdmin,
            _createdBy);

        // Assert
        user.Should().NotBeNull();
        user.PhoneNumber.Should().BeNull();
        user.AegisEmployeeId.Should().BeNull();
        user.AegisDepartment.Should().BeNull();
        user.IsAegisUser.Should().BeTrue();
    }

    #endregion

    #region Status Management Tests

    [Fact]
    public void Activate_WhenUserIsPendingActivation_ShouldActivateUser()
    {
        // Arrange
        var user = CreateTestUser();
        // Use property setter via reflection since Status has private setter
        var statusProperty = user.GetType().GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        statusProperty?.SetValue(user, UserStatus.PendingActivation);

        // Act
        user.Activate(_updatedBy);

        // Assert
        user.Status.Should().Be(UserStatus.Active);
        user.DomainEvents.Should().Contain(e => e is UserActivatedEvent);
    }

    [Fact]
    public void Activate_WhenUserIsAlreadyActive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.Activate(_updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("User is already active");
    }

    [Fact]
    public void Deactivate_WithValidReason_ShouldDeactivateUser()
    {
        // Arrange
        var user = CreateTestUser();
        var reason = "Policy violation";

        // Act
        user.Deactivate(_updatedBy, reason);

        // Assert
        user.Status.Should().Be(UserStatus.Inactive);
        user.DomainEvents.Should().Contain(e => e is UserDeactivatedEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Deactivate_WithInvalidReason_ShouldThrowArgumentException(string? reason)
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.Deactivate(_updatedBy, reason!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Deactivation reason is required*");
    }

    [Fact]
    public void Deactivate_WhenUserIsAlreadyInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate(_updatedBy, "Test reason");

        // Act & Assert
        var action = () => user.Deactivate(_updatedBy, "Another reason");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("User is already inactive");
    }

    #endregion

    #region Password Management Tests

    [Fact]
    public void ChangePassword_WithValidPassword_ShouldUpdatePassword()
    {
        // Arrange
        var user = CreateTestUser();
        var newPassword = PasswordHash.Create("NewPassword123!");

        // Act
        user.ChangePassword(newPassword, _updatedBy);

        // Assert
        user.PasswordHash.Should().Be(newPassword);
        user.PasswordChangedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        user.MustChangePassword.Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedOutUntil.Should().BeNull();
        user.DomainEvents.Should().Contain(e => e is UserPasswordChangedEvent);
    }

    [Fact]
    public void ChangePassword_WithValidPasswordAndReset_ShouldUpdatePasswordWithResetFlag()
    {
        // Arrange
        var user = CreateTestUser();
        var newPassword = PasswordHash.Create("NewPassword123!");

        // Act
        user.ChangePassword(newPassword, _updatedBy, isReset: true);

        // Assert
        user.PasswordHash.Should().Be(newPassword);
    }

    [Fact]
    public void ChangePassword_WithoutChangedBy_ShouldUpdatePassword()
    {
        // Arrange
        var user = CreateTestUser();
        var newPassword = PasswordHash.Create("NewPassword123!");

        // Act
        user.ChangePassword(newPassword, isReset: true);

        // Assert
        user.PasswordHash.Should().Be(newPassword);
    }

    [Fact]
    public void ChangePassword_WithNullPassword_ShouldThrowArgumentNullException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.ChangePassword(null!, _updatedBy);
        action.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Profile Management Tests

    [Fact]
    public void UpdateProfile_WithValidData_ShouldUpdateProfile()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.UpdateProfile("Jane", "Smith", _updatedBy, "+9876543210");

        // Assert
        user.FirstName.Should().Be("Jane");
        user.LastName.Should().Be("Smith");
        user.PhoneNumber.Should().Be("+9876543210");
        user.DomainEvents.Should().Contain(e => e is UserProfileUpdatedEvent);
    }

    [Fact]
    public void UpdateProfile_WithSameData_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var initialEventCount = user.DomainEvents.Count();

        // Act
        user.UpdateProfile(user.FirstName, user.LastName, _updatedBy, user.PhoneNumber);

        // Assert
        user.DomainEvents.Should().HaveCount(initialEventCount);
    }

    [Theory]
    [InlineData("", "Smith")]
    [InlineData("Jane", "")]
    [InlineData(null, "Smith")]
    [InlineData("Jane", null)]
    [InlineData("   ", "Smith")]
    [InlineData("Jane", "   ")]
    public void UpdateProfile_WithInvalidNames_ShouldThrowArgumentException(string? firstName, string? lastName)
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.UpdateProfile(firstName!, lastName!, _updatedBy);
        action.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Deletion Tests

    [Fact]
    public void Delete_WithValidReason_ShouldRaiseDeletionEvent()
    {
        // Arrange
        var user = CreateTestUser();
        var reason = "Account closure requested";

        // Act
        user.Delete(_updatedBy, reason);

        // Assert
        user.DomainEvents.Should().Contain(e => e is UserDeletedEvent);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Delete_WithInvalidReason_ShouldThrowArgumentException(string? reason)
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.Delete(_updatedBy, reason);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Deletion reason is required*");
    }

    #endregion

    #region Business and Branch Management Tests

    [Fact]
    public void IsInBusiness_WithMatchingBusinessId_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        user.IsInBusiness(_businessId).Should().BeTrue();
    }

    [Fact]
    public void IsInBusiness_WithDifferentBusinessId_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        user.IsInBusiness(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsInBusinessBranch_WithMatchingBranchId_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUserWithBranch();

        // Act & Assert
        user.IsInBusinessBranch(_branchId).Should().BeTrue();
    }

    [Fact]
    public void IsInBusinessBranch_WithDifferentBranchId_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUserWithBranch();

        // Act & Assert
        user.IsInBusinessBranch(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsBusinessLevelUser_WithNoBranch_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        user.IsBusinessLevelUser().Should().BeTrue();
    }

    [Fact]
    public void IsBusinessLevelUser_WithBranch_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUserWithBranch();

        // Act & Assert
        user.IsBusinessLevelUser().Should().BeFalse();
    }

    [Fact]
    public void IsBranchLevelUser_WithBranch_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUserWithBranch();

        // Act & Assert
        user.IsBranchLevelUser().Should().BeTrue();
    }

    [Fact]
    public void IsBranchLevelUser_WithNoBranch_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        user.IsBranchLevelUser().Should().BeFalse();
    }

    [Fact]
    public void TransferToBranch_WithValidBranch_ShouldTransferUser()
    {
        // Arrange
        var user = CreateTestUser();
        var newBranchId = Guid.NewGuid();

        // Act
        user.TransferToBranch(newBranchId, _updatedBy);

        // Assert
        user.BranchId.Should().Be(newBranchId);
        user.DomainEvents.Should().Contain(e => e is UserBranchAssignedEvent);
    }

    [Fact]
    public void TransferToBranch_WithSameBranch_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUserWithBranch();

        // Act & Assert
        var action = () => user.TransferToBranch(_branchId, _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("User is already assigned to this branch");
    }

    [Fact]
    public void RemoveFromBranch_WhenUserHasBranch_ShouldRemoveFromBranch()
    {
        // Arrange
        var user = CreateTestUserWithBranch();

        // Act
        user.RemoveFromBranch(_updatedBy);

        // Assert
        user.BranchId.Should().BeNull();
        user.DomainEvents.Should().Contain(e => e is UserBranchRemovedEvent);
    }

    [Fact]
    public void RemoveFromBranch_WhenUserHasNoBranch_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.RemoveFromBranch(_updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("User is not assigned to any branch");
    }

    #endregion

    #region Login and Security Tests

    [Fact]
    public void IsLocked_WhenNotLockedOut_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        user.IsLocked().Should().BeFalse();
    }

    [Fact]
    public void IsLocked_WhenLockedOutInFuture_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser();
        // Use reflection to set LockedOutUntil
        user.GetType().GetProperty("LockedOutUntil")?.SetValue(user, DateTimeOffset.UtcNow.AddMinutes(10));

        // Act & Assert
        user.IsLocked().Should().BeTrue();
    }

    [Fact]
    public void IsLocked_WhenLockedOutInPast_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser();
        user.GetType().GetProperty("LockedOutUntil")?.SetValue(user, DateTimeOffset.UtcNow.AddMinutes(-10));

        // Act & Assert
        user.IsLocked().Should().BeFalse();
    }

    [Fact]
    public void CanLogin_WhenActiveAndNotLocked_ShouldReturnTrue()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        user.CanLogin().Should().BeTrue();
    }

    [Fact]
    public void CanLogin_WhenInactive_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser();
        user.Deactivate(_updatedBy, "Test");

        // Act & Assert
        user.CanLogin().Should().BeFalse();
    }

    [Fact]
    public void RecordFailedLogin_ShouldIncrementFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        user.RecordFailedLogin("192.168.1.1");

        // Assert
        user.FailedLoginAttempts.Should().Be(1);
        user.LockedOutUntil.Should().BeNull();
    }

    [Fact]
    public void RecordFailedLogin_AfterFiveAttempts_ShouldLockUser()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        for (int i = 0; i < 5; i++)
        {
            user.RecordFailedLogin("192.168.1.1");
        }

        // Assert
        user.FailedLoginAttempts.Should().Be(5);
        user.LockedOutUntil.Should().NotBeNull();
        user.LockedOutUntil.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldResetFailedAttempts()
    {
        // Arrange
        var user = CreateTestUser();
        user.RecordFailedLogin("192.168.1.1");
        user.RecordFailedLogin("192.168.1.1");

        // Act
        user.RecordSuccessfulLogin("192.168.1.1");

        // Assert
        user.LastLoginAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        user.FailedLoginAttempts.Should().Be(0);
        user.LockedOutUntil.Should().BeNull();
    }

    #endregion

    #region Role Management Tests

    [Fact]
    public void AssignRole_WithValidRole_ShouldAssignRole()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();

        // Act
        user.AssignRole(roleId, _updatedBy);

        // Assert
        user.HasRole(roleId).Should().BeTrue();
        user.GetActiveRoles().Should().Contain(roleId);
        user.DomainEvents.Should().Contain(e => e is UserRoleAssignedEvent);
    }

    [Fact]
    public void AssignRole_WithExpirationDate_ShouldAssignRoleWithExpiration()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        user.AssignRole(roleId, _updatedBy, expiresAt);

        // Assert
        user.HasRole(roleId).Should().BeTrue();
    }

    [Fact]
    public void AssignRole_WhenRoleAlreadyAssigned_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId, _updatedBy);

        // Act & Assert
        var action = () => user.AssignRole(roleId, _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("User already has this role assigned");
    }

    [Fact]
    public void RevokeRole_WithValidRole_ShouldRevokeRole()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId, _updatedBy);

        // Act
        user.RevokeRole(roleId, _updatedBy, "No longer needed");

        // Assert
        user.HasRole(roleId).Should().BeFalse();
        user.GetActiveRoles().Should().NotContain(roleId);
        user.DomainEvents.Should().Contain(e => e is UserRoleRemovedEvent);
    }

    [Fact]
    public void RevokeRole_WhenRoleNotAssigned_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();

        // Act & Assert
        var action = () => user.RevokeRole(roleId, _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("User does not have this role assigned or it's already revoked");
    }

    [Fact]
    public void HasRole_WithNonAssignedRole_ShouldReturnFalse()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId = Guid.NewGuid();

        // Act & Assert
        user.HasRole(roleId).Should().BeFalse();
    }

    [Fact]
    public void GetActiveRoles_WithMultipleRoles_ShouldReturnActiveRoles()
    {
        // Arrange
        var user = CreateTestUser();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();
        var roleId3 = Guid.NewGuid();

        user.AssignRole(roleId1, _updatedBy);
        user.AssignRole(roleId2, _updatedBy);
        user.AssignRole(roleId3, _updatedBy);
        user.RevokeRole(roleId2, _updatedBy);

        // Act
        var activeRoles = user.GetActiveRoles().ToList();

        // Assert
        activeRoles.Should().HaveCount(2);
        activeRoles.Should().Contain(roleId1);
        activeRoles.Should().Contain(roleId3);
        activeRoles.Should().NotContain(roleId2);
    }

    #endregion

    #region Aegis User Tests

    [Fact]
    public void UpdateAegisActivity_WhenAegisUser_ShouldUpdateActivity()
    {
        // Arrange
        var user = CreateTestAegisUser();

        // Act
        user.UpdateAegisActivity();

        // Assert
        user.LastAegisActivityAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void UpdateAegisActivity_WhenNotAegisUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.UpdateAegisActivity();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Only KMPG users can have activity updated");
    }

    [Fact]
    public void UpdateAegisRole_WhenAegisUser_ShouldUpdateRole()
    {
        // Arrange
        var user = CreateTestAegisUser();

        // Act
        user.UpdateAegisRole(AegisRole.AegisAdmin, _updatedBy);

        // Assert
        user.AegisRole.Should().Be(AegisRole.AegisAdmin);
        user.DomainEvents.Should().Contain(e => e is UserAegisRoleChangedEvent);
    }

    [Fact]
    public void UpdateAegisRole_WhenNotAegisUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.UpdateAegisRole(AegisRole.AegisAdmin, _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Only KMPG users can have KMPG roles updated");
    }

    [Fact]
    public void UpdateKmpgProfile_WhenKmpgUser_ShouldUpdateProfile()
    {
        // Arrange
        var user = CreateTestAegisUser();

        // Act
        user.UpdateAegisProfile("EMP002", "IT Department", _updatedBy);

        // Assert
        user.AegisEmployeeId.Should().Be("EMP002");
        user.AegisDepartment.Should().Be("IT Department");
        user.DomainEvents.Should().Contain(e => e is UserAegisProfileUpdatedEvent);
    }

    [Fact]
    public void UpdateKmpgProfile_WithSameData_ShouldNotRaiseDomainEvent()
    {
        // Arrange
        var user = CreateTestAegisUser();
        var initialEventCount = user.DomainEvents.Count();

        // Act
        user.UpdateAegisProfile(user.AegisEmployeeId, user.AegisDepartment, _updatedBy);

        // Assert
        user.DomainEvents.Should().HaveCount(initialEventCount);
    }

    [Fact]
    public void UpdateKmpgProfile_WhenNotKmpgUser_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = CreateTestUser();

        // Act & Assert
        var action = () => user.UpdateAegisProfile("EMP001", "Finance", _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Only KMPG users can have KMPG profile updated");
    }

    #endregion

    #region ToString Tests

    [Fact]
    public void ToString_ShouldReturnFullName()
    {
        // Arrange
        var user = CreateTestUser();

        // Act
        var result = user.ToString();

        // Assert
        result.Should().Be("John Doe");
    }

    #endregion

    #region Helper Methods

    private User CreateTestUser()
    {
        return User.Create(
            _businessId,
            "John",
            "Doe",
            "john.doe@example.com",
            _passwordHash,
            _createdBy);
    }

    private User CreateTestUserWithBranch()
    {
        return User.Create(
            _businessId,
            "John",
            "Doe",
            "john.doe@example.com",
            _passwordHash,
            _createdBy,
            _branchId);
    }

    private User CreateTestAegisUser()
    {
        return User.CreateAegisUser(
            "Jane",
            "Smith",
            "jane.smith@Aegis.com",
            _passwordHash,
            AegisRole.AegisAdmin,
            _createdBy,
            "+1234567890",
            "EMP001",
            "Finance");
    }

    #endregion
}