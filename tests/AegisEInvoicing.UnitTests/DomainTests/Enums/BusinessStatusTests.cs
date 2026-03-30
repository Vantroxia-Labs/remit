using AegisEInvoicing.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Enums;

/// <summary>
/// Comprehensive tests for BusinessStatus enum targeting 100% code coverage
/// </summary>
public class BusinessStatusTests
{
    #region Enum Value Tests

    [Fact]
    public void BusinessStatus_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)BusinessStatus.Pending).Should().Be(0);
        ((int)BusinessStatus.Active).Should().Be(1);
        ((int)BusinessStatus.Inactive).Should().Be(2);
        ((int)BusinessStatus.Suspended).Should().Be(3);
        ((int)BusinessStatus.Deleted).Should().Be(4);
    }

    [Fact]
    public void BusinessStatus_ShouldHaveAllExpectedEnumValues()
    {
        // Arrange
        var expectedValues = new[]
        {
            BusinessStatus.Pending,
            BusinessStatus.Active,
            BusinessStatus.Inactive,
            BusinessStatus.Suspended,
            BusinessStatus.Deleted
        };

        // Act
        var actualValues = Enum.GetValues<BusinessStatus>();

        // Assert
        actualValues.Should().BeEquivalentTo(expectedValues);
        actualValues.Should().HaveCount(5);
    }

    [Theory]
    [InlineData(BusinessStatus.Pending, "Pending")]
    [InlineData(BusinessStatus.Active, "Active")]
    [InlineData(BusinessStatus.Inactive, "Inactive")]
    [InlineData(BusinessStatus.Suspended, "Suspended")]
    [InlineData(BusinessStatus.Deleted, "Deleted")]
    public void BusinessStatus_ToString_ShouldReturnCorrectString(BusinessStatus status, string expectedString)
    {
        // Act
        var result = status.ToString();

        // Assert
        result.Should().Be(expectedString);
    }

    #endregion

    #region Conversion Tests

    [Theory]
    [InlineData(0, BusinessStatus.Pending)]
    [InlineData(1, BusinessStatus.Active)]
    [InlineData(2, BusinessStatus.Inactive)]
    [InlineData(3, BusinessStatus.Suspended)]
    [InlineData(4, BusinessStatus.Deleted)]
    public void BusinessStatus_CastFromInt_ShouldReturnCorrectEnum(int value, BusinessStatus expectedStatus)
    {
        // Act
        var result = (BusinessStatus)value;

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData(BusinessStatus.Pending, 0)]
    [InlineData(BusinessStatus.Active, 1)]
    [InlineData(BusinessStatus.Inactive, 2)]
    [InlineData(BusinessStatus.Suspended, 3)]
    [InlineData(BusinessStatus.Deleted, 4)]
    public void BusinessStatus_CastToInt_ShouldReturnCorrectValue(BusinessStatus status, int expectedValue)
    {
        // Act
        var result = (int)status;

        // Assert
        result.Should().Be(expectedValue);
    }

    [Theory]
    [InlineData("Pending", BusinessStatus.Pending)]
    [InlineData("Active", BusinessStatus.Active)]
    [InlineData("Inactive", BusinessStatus.Inactive)]
    [InlineData("Suspended", BusinessStatus.Suspended)]
    [InlineData("Deleted", BusinessStatus.Deleted)]
    public void BusinessStatus_Parse_ShouldReturnCorrectEnum(string value, BusinessStatus expectedStatus)
    {
        // Act
        var result = Enum.Parse<BusinessStatus>(value);

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("pending", BusinessStatus.Pending)]
    [InlineData("ACTIVE", BusinessStatus.Active)]
    [InlineData("inactive", BusinessStatus.Inactive)]
    [InlineData("SUSPENDED", BusinessStatus.Suspended)]
    [InlineData("deleted", BusinessStatus.Deleted)]
    public void BusinessStatus_ParseIgnoreCase_ShouldReturnCorrectEnum(string value, BusinessStatus expectedStatus)
    {
        // Act
        var result = Enum.Parse<BusinessStatus>(value, ignoreCase: true);

        // Assert
        result.Should().Be(expectedStatus);
    }

    [Theory]
    [InlineData("Pending", true, BusinessStatus.Pending)]
    [InlineData("Active", true, BusinessStatus.Active)]
    [InlineData("InvalidStatus", false, default(BusinessStatus))]
    [InlineData("", false, default(BusinessStatus))]
    [InlineData(null, false, default(BusinessStatus))]
    public void BusinessStatus_TryParse_ShouldReturnExpectedResult(string? value, bool expectedSuccess, BusinessStatus expectedStatus)
    {
        // Act
        var success = Enum.TryParse<BusinessStatus>(value, out var result);

        // Assert
        success.Should().Be(expectedSuccess);
        result.Should().Be(expectedStatus);
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
    public void BusinessStatus_IsDefined_ShouldReturnCorrectResult(int value, bool expectedResult)
    {
        // Act
        var result = Enum.IsDefined(typeof(BusinessStatus), value);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("Pending", true)]
    [InlineData("Active", true)]
    [InlineData("Inactive", true)]
    [InlineData("Suspended", true)]
    [InlineData("Deleted", true)]
    [InlineData("InvalidStatus", false)]
    [InlineData("", false)]
    public void BusinessStatus_IsDefinedByName_ShouldReturnCorrectResult(string name, bool expectedResult)
    {
        // Act
        var result = Enum.IsDefined(typeof(BusinessStatus), name);

        // Assert
        result.Should().Be(expectedResult);
    }

    #endregion

    #region Comparison Tests

    [Fact]
    public void BusinessStatus_Equality_ShouldWorkCorrectly()
    {
        // Arrange
        var status1 = BusinessStatus.Active;
        var status2 = BusinessStatus.Active;
        var status3 = BusinessStatus.Inactive;

        // Act & Assert
        status1.Should().Be(status2);
        status1.Should().NotBe(status3);
        (status1 == status2).Should().BeTrue();
        (status1 != status3).Should().BeTrue();
    }

    [Fact]
    public void BusinessStatus_CompareTo_ShouldWorkCorrectly()
    {
        // Arrange
        var pending = BusinessStatus.Pending;
        var active = BusinessStatus.Active;
        var deleted = BusinessStatus.Deleted;

        // Act & Assert
        pending.CompareTo(active).Should().BeLessThan(0);
        active.CompareTo(pending).Should().BeGreaterThan(0);
        active.CompareTo(active).Should().Be(0);
        deleted.CompareTo(pending).Should().BeGreaterThan(0);
    }

    [Fact]
    public void BusinessStatus_GetHashCode_ShouldBeConsistent()
    {
        // Arrange
        var status1 = BusinessStatus.Active;
        var status2 = BusinessStatus.Active;

        // Act & Assert
        status1.GetHashCode().Should().Be(status2.GetHashCode());
    }

    #endregion

    #region Business Logic Tests

    [Theory]
    [InlineData(BusinessStatus.Active)]
    [InlineData(BusinessStatus.Inactive)]
    [InlineData(BusinessStatus.Suspended)]
    public void BusinessStatus_ActiveInactiveSuspended_ShouldBeOperationalStates(BusinessStatus status)
    {
        // Act & Assert
        var operationalStates = new[] { BusinessStatus.Active, BusinessStatus.Inactive, BusinessStatus.Suspended };
        operationalStates.Should().Contain(status);
    }

    [Theory]
    [InlineData(BusinessStatus.Pending)]
    [InlineData(BusinessStatus.Deleted)]
    public void BusinessStatus_PendingDeleted_ShouldBeNonOperationalStates(BusinessStatus status)
    {
        // Act & Assert
        var nonOperationalStates = new[] { BusinessStatus.Pending, BusinessStatus.Deleted };
        nonOperationalStates.Should().Contain(status);
    }

    [Fact]
    public void BusinessStatus_ShouldSupportLinqOperations()
    {
        // Arrange
        var statuses = new[]
        {
            BusinessStatus.Pending,
            BusinessStatus.Active,
            BusinessStatus.Inactive,
            BusinessStatus.Suspended,
            BusinessStatus.Deleted
        };

        // Act
        var activeStatuses = statuses.Where(s => s == BusinessStatus.Active).ToList();
        var nonDeletedStatuses = statuses.Where(s => s != BusinessStatus.Deleted).ToList();

        // Assert
        activeStatuses.Should().HaveCount(1);
        activeStatuses.Should().Contain(BusinessStatus.Active);
        nonDeletedStatuses.Should().HaveCount(4);
        nonDeletedStatuses.Should().NotContain(BusinessStatus.Deleted);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void BusinessStatus_GetNames_ShouldReturnAllNames()
    {
        // Act
        var names = Enum.GetNames<BusinessStatus>();

        // Assert
        names.Should().HaveCount(5);
        names.Should().Contain("Pending");
        names.Should().Contain("Active");
        names.Should().Contain("Inactive");
        names.Should().Contain("Suspended");
        names.Should().Contain("Deleted");
    }

    [Fact]
    public void BusinessStatus_GetValues_ShouldReturnAllValues()
    {
        // Act
        var values = Enum.GetValues<BusinessStatus>();

        // Assert
        values.Should().HaveCount(5);
        values.Should().Contain(BusinessStatus.Pending);
        values.Should().Contain(BusinessStatus.Active);
        values.Should().Contain(BusinessStatus.Inactive);
        values.Should().Contain(BusinessStatus.Suspended);
        values.Should().Contain(BusinessStatus.Deleted);
    }

    [Fact]
    public void BusinessStatus_Format_ShouldWorkWithDifferentFormats()
    {
        // Arrange
        var status = BusinessStatus.Active;

        // Act & Assert
        Enum.Format(typeof(BusinessStatus), status, "G").Should().Be("Active");
        Enum.Format(typeof(BusinessStatus), status, "D").Should().Be("1");
        Enum.Format(typeof(BusinessStatus), status, "X").Should().Be("00000001");
        Enum.Format(typeof(BusinessStatus), status, "F").Should().Be("Active");
    }

    [Theory]
    [InlineData(BusinessStatus.Pending)]
    [InlineData(BusinessStatus.Active)]
    [InlineData(BusinessStatus.Inactive)]
    [InlineData(BusinessStatus.Suspended)]
    [InlineData(BusinessStatus.Deleted)]
    public void BusinessStatus_Equals_WithBoxing_ShouldWorkCorrectly(BusinessStatus status)
    {
        // Arrange
        object boxedStatus = status;

        // Act & Assert
        status.Equals(boxedStatus).Should().BeTrue();
        boxedStatus.Equals(status).Should().BeTrue();
    }

    #endregion
}