using AegisEInvoicing.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class FIRSApiConfigurationTests
{
    private readonly string _validName = "Aegis SaaS Configuration";
    private readonly string _validDescription = "Default FIRS API configuration for SaaS deployment";
    private readonly string _validEncryptedApiKey = "encrypted_api_key_123";
    private readonly string _validEncryptedApiSecret = "encrypted_api_secret_456";
    private readonly string _validEnv = "Development";
    private readonly string _validBaseUrl = "https://www.e-invoice.gov.ng";

    [Fact]
    public void CreateForSaaS_WithValidParameters_ShouldCreateSaaSConfiguration()
    {
        // Act
        var config = FIRSApiConfiguration.CreateForSaaS(
            _validName,
            _validDescription,
            _validEncryptedApiKey,
            _validEncryptedApiSecret,
            _validEnv,
            _validBaseUrl);

        // Assert
        config.Should().NotBeNull();
        config.Name.Should().Be(_validName);
        config.Description.Should().Be(_validDescription);
        config.DeploymentType.Should().Be(FIRSDeploymentType.SaaS);
        config.EncryptedApiKey.Should().Be(_validEncryptedApiKey);
        config.EncryptedApiSecret.Should().Be(_validEncryptedApiSecret);
        config.IsActive.Should().BeTrue();
        config.IsDefault.Should().BeFalse();
        config.BusinessFIRSApiConfigurations.Should().BeEmpty();
    }

    [Fact]
    public void CreateForOnPremise_WithValidParameters_ShouldCreateOnPremiseConfiguration()
    {
        // Arrange
        var name = "Client On-Premise Configuration";
        var description = "Customer-managed FIRS API configuration";

        // Act
        var config = FIRSApiConfiguration.CreateForOnPremise(
            name,
            description,
            _validEncryptedApiKey,
            _validEncryptedApiSecret,
            _validEnv,
            _validBaseUrl);

        // Assert
        config.Should().NotBeNull();
        config.Name.Should().Be(name);
        config.Description.Should().Be(description);
        config.DeploymentType.Should().Be(FIRSDeploymentType.OnPremise);
        config.EncryptedApiKey.Should().Be(_validEncryptedApiKey);
        config.EncryptedApiSecret.Should().Be(_validEncryptedApiSecret);
        config.IsActive.Should().BeTrue();
        config.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void SetAsDefault_ShouldSetIsDefaultToTrue()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();

        // Act
        config.SetAsDefault();

        // Assert
        config.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void RemoveAsDefault_ShouldSetIsDefaultToFalse()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();
        config.SetAsDefault();

        // Act
        config.RemoveAsDefault();

        // Assert
        config.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();
        config.Deactivate();

        // Act
        config.Activate();

        // Assert
        config.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();

        // Act
        config.Deactivate();

        // Assert
        config.IsActive.Should().BeFalse();
    }

    [Fact]
    public void UpdateCredentials_ShouldUpdateAllCredentialFields()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();
        var newName = "Updated Configuration";
        var newDescription = "Updated description";
        var newEncryptedApiKey = "new_encrypted_api_key";
        var newEncryptedApiSecret = "new_encrypted_api_secret";
        var newValidEnv = "Staging";
        var newValidBaseUrl = "https://www.e-invoice-dev.gov.ng";

    // Act
    config.UpdateCredentials(newName, newDescription, 
                newEncryptedApiKey, newEncryptedApiSecret, 
                newValidEnv, newValidBaseUrl);

        // Assert
        config.Name.Should().Be(newName);
        config.Description.Should().Be(newDescription);
        config.EncryptedApiKey.Should().Be(newEncryptedApiKey);
        config.EncryptedApiSecret.Should().Be(newEncryptedApiSecret);
    }

    [Fact]
    public void BusinessFIRSApiConfigurations_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();

        // Act & Assert
        config.BusinessFIRSApiConfigurations.Should().NotBeNull();
        config.BusinessFIRSApiConfigurations.Should().BeEmpty();
        config.BusinessFIRSApiConfigurations.Should().BeAssignableTo<IReadOnlyCollection<AegisEInvoicing.Domain.Entities.BusinessManagement.BusinessFIRSApiConfiguration>>();
    }

    [Theory]
    [InlineData(FIRSDeploymentType.SaaS, "SaaS Configuration")]
    [InlineData(FIRSDeploymentType.OnPremise, "OnPremise Configuration")]
    public void Create_WithDifferentDeploymentTypes_ShouldSetCorrectDeploymentType(FIRSDeploymentType deploymentType, string name)
    {
        // Act
        FIRSApiConfiguration config = deploymentType == FIRSDeploymentType.SaaS
            ? FIRSApiConfiguration.CreateForSaaS(name, "Description", _validEncryptedApiKey, _validEncryptedApiSecret, _validEnv, _validBaseUrl)
            : FIRSApiConfiguration.CreateForOnPremise(name, "Description", _validEncryptedApiKey, _validEncryptedApiSecret, _validEnv, _validBaseUrl);

        // Assert
        config.DeploymentType.Should().Be(deploymentType);
        config.Name.Should().Be(name);
    }

    [Fact]
    public void FIRSDeploymentType_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)FIRSDeploymentType.SaaS).Should().Be(1);
        ((int)FIRSDeploymentType.OnPremise).Should().Be(2);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateCredentials_WithWhitespaceValues_ShouldStillUpdate(string whitespaceValue)
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();

        // Act
        config.UpdateCredentials(whitespaceValue, whitespaceValue, 
                                 whitespaceValue, whitespaceValue, 
                                 whitespaceValue, whitespaceValue);

        // Assert
        config.Name.Should().Be(whitespaceValue);
        config.Description.Should().Be(whitespaceValue);
        config.EncryptedApiKey.Should().Be(whitespaceValue);
        config.EncryptedApiSecret.Should().Be(whitespaceValue);
    }

    [Fact]
    public void Create_NewConfiguration_ShouldHaveCorrectInitialState()
    {
        // Act
        var config = CreateTestSaaSConfiguration();

        // Assert
        config.IsActive.Should().BeTrue();
        config.IsDefault.Should().BeFalse();
        config.BusinessFIRSApiConfigurations.Should().BeEmpty();
    }

    [Fact]
    public void SetAsDefault_ThenRemoveAsDefault_ShouldToggleCorrectly()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();

        // Act & Assert
        config.IsDefault.Should().BeFalse();

        config.SetAsDefault();
        config.IsDefault.Should().BeTrue();

        config.RemoveAsDefault();
        config.IsDefault.Should().BeFalse();
    }

    [Fact]
    public void Activate_ThenDeactivate_ShouldToggleCorrectly()
    {
        // Arrange
        var config = CreateTestSaaSConfiguration();

        // Act & Assert
        config.IsActive.Should().BeTrue();

        config.Deactivate();
        config.IsActive.Should().BeFalse();

        config.Activate();
        config.IsActive.Should().BeTrue();
    }

    private FIRSApiConfiguration CreateTestSaaSConfiguration()
    {
        return FIRSApiConfiguration.CreateForSaaS(
            _validName,
            _validDescription,
            _validEncryptedApiKey,
            _validEncryptedApiSecret,
            _validEnv,
            _validBaseUrl);
    }
}