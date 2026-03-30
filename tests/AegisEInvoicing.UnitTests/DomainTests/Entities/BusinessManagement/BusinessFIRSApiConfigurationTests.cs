using AegisEInvoicing.Domain.Entities.BusinessManagement;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.BusinessManagement;

public class BusinessFIRSApiConfigurationTests
{
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _firsApiConfigurationId = Guid.NewGuid();

    [Fact]
    public void Create_WithValidParameters_ShouldCreateBusinessFIRSApiConfiguration()
    {
        // Act
        var businessConfig = BusinessFIRSApiConfiguration.Create(_businessId, _firsApiConfigurationId);

        // Assert
        businessConfig.Should().NotBeNull();
        businessConfig.BusinessId.Should().Be(_businessId);
        businessConfig.FIRSApiConfigurationId.Should().Be(_firsApiConfigurationId);
    }

    [Fact]
    public void Update_WithValidFIRSApiConfigurationId_ShouldUpdateConfiguration()
    {
        // Arrange
        var businessConfig = BusinessFIRSApiConfiguration.Create(_businessId, _firsApiConfigurationId);
        var newFirsApiConfigurationId = Guid.NewGuid();

        // Act
        businessConfig.Update(newFirsApiConfigurationId);

        // Assert
        businessConfig.FIRSApiConfigurationId.Should().Be(newFirsApiConfigurationId);
        businessConfig.BusinessId.Should().Be(_businessId); // Should remain unchanged
    }

    [Fact]
    public void Update_WithSameFIRSApiConfigurationId_ShouldStillUpdate()
    {
        // Arrange
        var businessConfig = BusinessFIRSApiConfiguration.Create(_businessId, _firsApiConfigurationId);

        // Act
        businessConfig.Update(_firsApiConfigurationId);

        // Assert
        businessConfig.FIRSApiConfigurationId.Should().Be(_firsApiConfigurationId);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("11111111-1111-1111-1111-111111111111")]
    public void Create_WithDifferentGuidValues_ShouldWork(string guidString)
    {
        // Arrange
        var testGuid = Guid.Parse(guidString);

        // Act
        var businessConfig = BusinessFIRSApiConfiguration.Create(_businessId, testGuid);

        // Assert
        businessConfig.FIRSApiConfigurationId.Should().Be(testGuid);
    }

    [Fact]
    public void Create_WithEmptyBusinessId_ShouldStillCreateConfiguration()
    {
        // Arrange
        var emptyBusinessId = Guid.Empty;

        // Act
        var businessConfig = BusinessFIRSApiConfiguration.Create(emptyBusinessId, _firsApiConfigurationId);

        // Assert
        businessConfig.BusinessId.Should().Be(Guid.Empty);
        businessConfig.FIRSApiConfigurationId.Should().Be(_firsApiConfigurationId);
    }

    [Fact]
    public void Create_WithEmptyFIRSApiConfigurationId_ShouldStillCreateConfiguration()
    {
        // Arrange
        var emptyConfigId = Guid.Empty;

        // Act
        var businessConfig = BusinessFIRSApiConfiguration.Create(_businessId, emptyConfigId);

        // Assert
        businessConfig.BusinessId.Should().Be(_businessId);
        businessConfig.FIRSApiConfigurationId.Should().Be(Guid.Empty);
    }
}