using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class BranchTests
{
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _createdBy = Guid.NewGuid();
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Address _validAddress;

    public BranchTests()
    {
        _validAddress = Address.Create("123 Branch St", "Lagos", "Lagos", "Nigeria", "100001");
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateBranch()
    {
        // Act
        var branch = Branch.Create(
            _businessId,
            "Main Branch",
            "MB001",
            _validAddress,
            "main@branch.com",
            "+234-800-123-4567",
            _createdBy);

        // Assert
        branch.Should().NotBeNull();
        branch.Id.Should().NotBeEmpty();
        branch.BusinessId.Should().Be(_businessId);
        branch.Name.Should().Be("Main Branch");
        branch.Code.Should().Be("MB001");
        branch.Address.Should().Be(_validAddress);
        branch.ContactEmail.Should().Be("main@branch.com");
        branch.ContactPhone.Should().Be("+234-800-123-4567");
        branch.IsActive.Should().BeTrue();
        branch.IsHeadOffice.Should().BeFalse();
        branch.AdminUserId.Should().BeNull();
        branch.CreatedBy.Should().Be(_createdBy);
        branch.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_WithHeadOfficeFlag_ShouldCreateHeadOfficeBranch()
    {
        // Act
        var branch = Branch.Create(
            _businessId,
            "Head Office",
            "HO001",
            _validAddress,
            "ho@company.com",
            "+234-800-123-4567",
            _createdBy,
            isHeadOffice: true);

        // Assert
        branch.IsHeadOffice.Should().BeTrue();
    }

    [Fact]
    public void Create_WithAdminUserId_ShouldAssignAdmin()
    {
        // Act
        var branch = Branch.Create(
            _businessId,
            "Admin Branch",
            "AB001",
            _validAddress,
            "admin@branch.com",
            "+234-800-123-4567",
            _createdBy,
            adminUserId: _adminUserId);

        // Assert
        branch.AdminUserId.Should().Be(_adminUserId);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var branch = CreateTestBranch();
        branch.Deactivate();

        // Act
        branch.Activate();

        // Assert
        branch.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var branch = CreateTestBranch();

        // Act
        branch.Deactivate();

        // Assert
        branch.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateContactInfo_WithValidInfo_ShouldUpdateEmailAndPhone()
    {
        // Arrange
        var branch = CreateTestBranch();
        var newEmail = "updated@branch.com";
        var newPhone = "+234-800-999-8888";

        // Act
        branch.UpdateContactInfo(newEmail, newPhone);

        // Assert
        branch.ContactEmail.Should().Be(newEmail);
        branch.ContactPhone.Should().Be(newPhone);
    }

    [Fact]
    public void SetAdmin_WithValidUserId_ShouldSetAdmin()
    {
        // Arrange
        var branch = CreateTestBranch();
        var newAdminUserId = Guid.NewGuid();
        var changedBy = Guid.NewGuid();

        // Act
        branch.SetAdmin(newAdminUserId, changedBy);

        // Assert
        branch.AdminUserId.Should().Be(newAdminUserId);
    }

    [Fact]
    public void RemoveAdmin_ShouldSetAdminUserIdToNull()
    {
        // Arrange
        var branch = CreateTestBranch();
        branch.SetAdmin(_adminUserId, _createdBy);
        var removedBy = Guid.NewGuid();

        // Act
        branch.RemoveAdmin(removedBy);

        // Assert
        branch.AdminUserId.Should().BeNull();
    }

    [Fact]
    public void IsAdminUser_WithMatchingUserId_ShouldReturnTrue()
    {
        // Arrange
        var branch = CreateTestBranch();
        branch.SetAdmin(_adminUserId, _createdBy);

        // Act & Assert
        branch.IsAdminUser(_adminUserId).Should().BeTrue();
    }

    [Fact]
    public void IsAdminUser_WithDifferentUserId_ShouldReturnFalse()
    {
        // Arrange
        var branch = CreateTestBranch();
        branch.SetAdmin(_adminUserId, _createdBy);

        // Act & Assert
        branch.IsAdminUser(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsAdminUser_WithNoAdminSet_ShouldReturnFalse()
    {
        // Arrange
        var branch = CreateTestBranch();

        // Act & Assert
        branch.IsAdminUser(_adminUserId).Should().BeFalse();
    }

    [Fact]
    public void IsUserInBranch_WithUserNotInBranch_ShouldReturnFalse()
    {
        // Arrange
        var branch = CreateTestBranch();
        var userId = Guid.NewGuid();

        // Act & Assert
        branch.IsUserInBranch(userId).Should().BeFalse();
    }

    [Fact]
    public void Users_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var branch = CreateTestBranch();

        // Act & Assert
        branch.Users.Should().NotBeNull();
        branch.Users.Should().BeEmpty();
        branch.Users.Should().BeAssignableTo<IReadOnlyCollection<AegisEInvoicing.Domain.Entities.UserManagement.User>>();
    }

    private Branch CreateTestBranch()
    {
        return Branch.Create(
            _businessId,
            "Test Branch",
            "TB001",
            _validAddress,
            "test@branch.com",
            "+234-800-123-4567",
            _createdBy);
    }
}