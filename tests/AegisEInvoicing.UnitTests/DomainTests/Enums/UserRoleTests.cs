using AegisEInvoicing.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Enums;

/// <summary>
/// Comprehensive tests for UserRole enum targeting 100% code coverage
/// </summary>
public class UserRoleTests
{
    #region Enum Value Tests

    [Fact]
    public void UserRole_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)UserRole.Admin).Should().Be(0);
        ((int)UserRole.Manager).Should().Be(1);
        ((int)UserRole.Accountant).Should().Be(2);
        ((int)UserRole.Auditor).Should().Be(3);
        ((int)UserRole.Viewer).Should().Be(4);
    }

    [Fact]
    public void UserRole_ShouldHaveAllExpectedEnumValues()
    {
        // Arrange
        var expectedValues = new[]
        {
            UserRole.Admin,
            UserRole.Manager,
            UserRole.Accountant,
            UserRole.Auditor,
            UserRole.Viewer
        };

        // Act
        var actualValues = Enum.GetValues<UserRole>();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
        actualValues.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(UserRole.Admin, "Admin")]
    [InlineData(UserRole.Manager, "Manager")]
    [InlineData(UserRole.Accountant, "Accountant")]
    [InlineData(UserRole.Auditor, "Auditor")]
    [InlineData(UserRole.Viewer, "Viewer")]
    public void UserRole_ToString_ShouldReturnCorrectString(UserRole role, string expectedString)
    {
        // Act
        var result = role.ToString();

        // Assert
        result.Should().Be(expectedString);
    }

    #endregion

    #region Conversion Tests

    [Theory]
    [InlineData(0, UserRole.Admin)]
    [InlineData(1, UserRole.Manager)]
    [InlineData(2, UserRole.Accountant)]
    [InlineData(3, UserRole.Auditor)]
    [InlineData(4, UserRole.Viewer)]
    public void UserRole_CastFromInt_ShouldReturnCorrectEnum(int value, UserRole expectedRole)
    {
        // Act
        var result = (UserRole)value;

        // Assert
        result.Should().Be(expectedRole);
    }

    [Theory]
    [InlineData(UserRole.Admin, 0)]
    [InlineData(UserRole.Manager, 1)]
    [InlineData(UserRole.Accountant, 2)]
    [InlineData(UserRole.Auditor, 3)]
    [InlineData(UserRole.Viewer, 4)]
    public void UserRole_CastToInt_ShouldReturnCorrectValue(UserRole role, int expectedValue)
    {
        // Act
        var result = (int)role;

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("Admin", UserRole.Admin)]
    [InlineData("Manager", UserRole.Manager)]
    [InlineData("Accountant", UserRole.Accountant)]
    [InlineData("Auditor", UserRole.Auditor)]
    [InlineData("Viewer", UserRole.Viewer)]
    public void UserRole_Parse_ShouldReturnCorrectEnum(string value, UserRole expectedRole)
    {
        // Act
        var result = Enum.Parse<UserRole>(value);

        // Assert
        result.Should().Be(expectedRole);
    }

    [Theory]
    [InlineData("admin", UserRole.Admin)]
    [InlineData("MANAGER", UserRole.Manager)]
    [InlineData("accountant", UserRole.Accountant)]
    [InlineData("AUDITOR", UserRole.Auditor)]
    [InlineData("viewer", UserRole.Viewer)]
    public void UserRole_ParseIgnoreCase_ShouldReturnCorrectEnum(string value, UserRole expectedRole)
    {
        // Act
        var result = Enum.Parse<UserRole>(value, ignoreCase: true);

        // Assert
        result.Should().Be(expectedRole);
    }

    [Theory]
    [InlineData("Admin", true, UserRole.Admin)]
    [InlineData("Manager", true, UserRole.Manager)]
    [InlineData("InvalidRole", false, default(UserRole))]
    [InlineData("", false, default(UserRole))]
    [InlineData(null, false, default(UserRole))]
    public void UserRole_TryParse_ShouldReturnExpectedResult(string? value, bool expectedSuccess, UserRole expectedRole)
    {
        // Act
        var success = Enum.TryParse<UserRole>(value, out var result);

        // Assert
        success.Should().Be(expectedSuccess);
        result.Should().Be(expectedRole);
    }

    #endregion

    #region IsDefined Tests

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(-1, false)]
    [InlineData(100, false)]
    public void UserRole_IsDefined_ShouldReturnCorrectResult(int value, bool expectedResult)
    {
        // Act
        var result = Enum.IsDefined(typeof(UserRole), value);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("Admin", true)]
    [InlineData("Manager", true)]
    [InlineData("Accountant", true)]
    [InlineData("Auditor", true)]
    [InlineData("Viewer", true)]
    [InlineData("InvalidRole", false)]
    [InlineData("", false)]
    public void UserRole_IsDefinedByName_ShouldReturnCorrectResult(string name, bool expectedResult)
    {
        // Act
        var result = Enum.IsDefined(typeof(UserRole), name);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void UserRole_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var role1 = UserRole.Admin;
        var role2 = UserRole.Admin;
        var role3 = UserRole.Manager;

        // Act & Assert
        role1.Should().Be(role2);
        role1.Should().NotBe(role3);
        (role1 == role2).Should().BeTrue();
        (role1 != role3).Should().BeTrue();
    }

    [Fact]
    public void UserRole_CompareTo_ShouldWorkCorrectly()
    {
        // Arrange
        var admin = UserRole.Admin;
        var manager = UserRole.Manager;
        var viewer = UserRole.Viewer;

        // Act & Assert
        admin.CompareTo(manager).Should().BeLessThan(0);
        manager.CompareTo(admin).Should().BeGreaterThan(0);
        admin.CompareTo(admin).Should().Be(0);
        viewer.CompareTo(admin).Should().BeGreaterThan(0);
    }

    [Fact]
    public void UserRole_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var role1 = UserRole.Admin;
        var role2 = UserRole.Admin;

        // Act & Assert
        role1.GetHashCode().Should().Be(role2.GetHashCode());
    }

    #endregion

    #region Business Logic Tests

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Manager)]
    public void UserRole_AdminManager_ShouldBeManagementRoles(UserRole role)
    {
        // Act & Assert
        var managementRoles = new[] { UserRole.Admin, UserRole.Manager };
        managementRoles.Should().Contain(role);
    }

    [Theory]
    [InlineData(UserRole.Accountant)]
    [InlineData(UserRole.Auditor)]
    [InlineData(UserRole.Viewer)]
    public void UserRole_AccountantAuditorViewer_ShouldBeOperationalRoles(UserRole role)
    {
        // Act & Assert
        var operationalRoles = new[] { UserRole.Accountant, UserRole.Auditor, UserRole.Viewer };
        operationalRoles.Should().Contain(role);
    }

    [Fact]
    public void UserRole_ShouldSupportHierarchicalSorting()
    {
        // Arrange
        var roles = new[]
        {
            UserRole.Viewer,
            UserRole.Admin,
            UserRole.Accountant,
            UserRole.Manager,
            UserRole.Auditor
        };

        // Act
        var sortedRoles = roles.OrderBy(r => (int)r).ToList();

        // Assert
        sortedRoles[0].Should().Be(UserRole.Admin);
        sortedRoles[1].Should().Be(UserRole.Manager);
        sortedRoles[2].Should().Be(UserRole.Accountant);
        sortedRoles[3].Should().Be(UserRole.Auditor);
        sortedRoles[4].Should().Be(UserRole.Viewer);
    }

    [Fact]
    public void UserRole_ShouldSupportLinqOperations()
    {
        // Arrange
        var roles = new[]
        {
            UserRole.Admin,
            UserRole.Manager,
            UserRole.Accountant,
            UserRole.Auditor,
            UserRole.Viewer
        };

        // Act
        var adminRoles = roles.Where(r => r == UserRole.Admin).ToList();
        var nonViewerRoles = roles.Where(r => r != UserRole.Viewer).ToList();

        // Assert
        adminRoles.Should().HaveCount(1);
        adminRoles.Should().Contain(UserRole.Admin);
        nonViewerRoles.Should().HaveCount(4);
        nonViewerRoles.Should().NotContain(UserRole.Viewer);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void UserRole_GetNames_ShouldReturnAllNames()
    {
        // Act
        var names = Enum.GetNames<UserRole>();

        // Assert
        names.Should().HaveCount(5);
        names.Should().Contain("Admin");
        names.Should().Contain("Manager");
        names.Should().Contain("Accountant");
        names.Should().Contain("Auditor");
        names.Should().Contain("Viewer");
    }

    [Fact]
    public void UserRole_GetValues_ShouldReturnAllValues()
    {
        // Act
        var values = Enum.GetValues<UserRole>();

        // Assert
        values.Should().HaveCount(5);
        values.Should().Contain(UserRole.Admin);
        values.Should().Contain(UserRole.Manager);
        values.Should().Contain(UserRole.Accountant);
        values.Should().Contain(UserRole.Auditor);
        values.Should().Contain(UserRole.Viewer);
    }

    [Fact]
    public void UserRole_Format_ShouldWorkWithDifferentFormats()
    {
        // Arrange
        var role = UserRole.Admin;

        // Act & Assert
        Enum.Format(typeof(UserRole), role, "G").Should().Be("Admin");
        Enum.Format(typeof(UserRole), role, "D").Should().Be("0");
        Enum.Format(typeof(UserRole), role, "X").Should().Be("00000000");
        Enum.Format(typeof(UserRole), role, "F").Should().Be("Admin");
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.Manager)]
    [InlineData(UserRole.Accountant)]
    [InlineData(UserRole.Auditor)]
    [InlineData(UserRole.Viewer)]
    public void UserRole_Equals_WithBoxing_ShouldWorkCorrectly(UserRole role)
    {
        // Arrange
        object boxedRole = role;

        // Act & Assert
        role.Equals(boxedRole).Should().BeTrue();
        boxedRole.Equals(role).Should().BeTrue();
    }

    [Fact]
    public void UserRole_HasFlags_Attribute_ShouldBeFalse()
    {
        // Act
        var hasFlags = typeof(UserRole).IsDefined(typeof(FlagsAttribute), false);

        // Assert
        hasFlags.Should().BeFalse();
    }

    [Fact]
    public void UserRole_UnderlyingType_ShouldBeInt32()
    {
        // Act
        var underlyingType = Enum.GetUnderlyingType(typeof(UserRole));

        // Assert
        underlyingType.Should().Be(typeof(int));
    }

    #endregion

    #region Permission Level Tests

    [Theory]
    [InlineData(UserRole.Admin, 0)] // Highest permissions
    [InlineData(UserRole.Manager, 1)]
    [InlineData(UserRole.Accountant, 2)]
    [InlineData(UserRole.Auditor, 3)]
    [InlineData(UserRole.Viewer, 4)] // Lowest permissions
    public void UserRole_PermissionLevel_ShouldReflectHierarchy(UserRole role, int expectedLevel)
    {
        // Act
        var level = (int)role;

        // Assert
        level.Should().Be(expectedLevel);
    }

    [Fact]
    public void UserRole_CanAccessHigherLevelFunctions_ShouldWorkCorrectly()
    {
        // Arrange
        var currentUserRole = UserRole.Manager;
        var requiredRole = UserRole.Accountant;

        // Act
        var canAccess = (int)currentUserRole <= (int)requiredRole;

        // Assert
        canAccess.Should().BeTrue(); // Manager (1) can access Accountant (2) level functions
    }

    #endregion
}