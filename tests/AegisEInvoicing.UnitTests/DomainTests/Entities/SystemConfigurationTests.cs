using AegisEInvoicing.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class SystemConfigurationTests
{
    private readonly Guid _setupBy = Guid.NewGuid();
    private readonly Guid _updatedBy = Guid.NewGuid();

    [Fact]
    public void CreateForSaaS_WithDefaultParameters_ShouldCreateSaaSConfiguration()
    {
        // Arrange
        var organizationName = "Aegis Nigeria";

        // Act
        var config = SystemConfiguration.CreateForSaaS(organizationName, _setupBy);

        // Assert
        config.Should().NotBeNull();
        config.Id.Should().NotBeEmpty();
        config.OrganizationName.Should().Be(organizationName);
        config.DeploymentMode.Should().Be(DeploymentMode.Cloud);
        config.AllowSelfOnboarding.Should().BeTrue();
        config.MaxBusinessesAllowed.Should().Be(1000);
        config.IsSetupCompleted.Should().BeTrue();
        config.SetupCompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        config.SetupCompletedBy.Should().Be(_setupBy);
        config.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        config.CreatedBy.Should().Be(_setupBy);

        // SaaS should not have license info
        config.LicenseKey.Should().BeNull();
        config.LicenseExpiryDate.Should().BeNull();
        config.OrganizationContactEmail.Should().BeNull();
        config.OrganizationContactPhone.Should().BeNull();
    }

    [Fact]
    public void CreateForSaaS_WithCustomParameters_ShouldCreateSaaSConfiguration()
    {
        // Arrange
        var organizationName = "Aegis Nigeria";
        var allowSelfOnboarding = false;
        var maxBusinesses = 500;

        // Act
        var config = SystemConfiguration.CreateForSaaS(
            organizationName,
            _setupBy,
            allowSelfOnboarding,
            maxBusinesses);

        // Assert
        config.AllowSelfOnboarding.Should().BeFalse();
        config.MaxBusinessesAllowed.Should().Be(500);
    }

    [Fact]
    public void CreateForOnPremise_WithValidParameters_ShouldCreateOnPremiseConfiguration()
    {
        // Arrange
        var organizationName = "Client Corporation";
        var licenseKey = "LICENSE-KEY-12345";
        var licenseExpiryDate = DateTimeOffset.UtcNow.AddYears(1);
        var contactEmail = "admin@client.com";
        var contactPhone = "+234-800-123-4567";

        // Act
        var config = SystemConfiguration.CreateForOnPremise(
            organizationName,
            licenseKey,
            licenseExpiryDate,
            contactEmail,
            contactPhone,
            _setupBy);

        // Assert
        config.Should().NotBeNull();
        config.Id.Should().NotBeEmpty();
        config.OrganizationName.Should().Be(organizationName);
        config.DeploymentMode.Should().Be(DeploymentMode.OnPremise);
        config.LicenseKey.Should().Be(licenseKey);
        config.LicenseExpiryDate.Should().Be(licenseExpiryDate);
        config.OrganizationContactEmail.Should().Be(contactEmail);
        config.OrganizationContactPhone.Should().Be(contactPhone);
        config.AllowSelfOnboarding.Should().BeFalse();
        config.MaxBusinessesAllowed.Should().Be(1);
        config.IsSetupCompleted.Should().BeTrue();
        config.SetupCompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        config.SetupCompletedBy.Should().Be(_setupBy);
        config.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        config.CreatedBy.Should().Be(_setupBy);
    }

    [Fact]
    public void UpdateLicense_WithOnPremiseDeployment_ShouldUpdateLicense()
    {
        // Arrange
        var config = CreateOnPremiseConfiguration();
        var newLicenseKey = "NEW-LICENSE-KEY-67890";
        var newExpiryDate = DateTimeOffset.UtcNow.AddYears(2);

        // Act
        config.UpdateLicense(newLicenseKey, newExpiryDate, _updatedBy);

        // Assert
        config.LicenseKey.Should().Be(newLicenseKey);
        config.LicenseExpiryDate.Should().Be(newExpiryDate);
        config.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        config.UpdatedBy.Should().Be(_updatedBy);
    }

    [Fact]
    public void UpdateLicense_WithSaaSDeployment_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var config = CreateSaaSConfiguration();
        var newLicenseKey = "NEW-LICENSE-KEY-67890";
        var newExpiryDate = DateTimeOffset.UtcNow.AddYears(2);

        // Act & Assert
        var action = () => config.UpdateLicense(newLicenseKey, newExpiryDate, _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("License can only be updated for On-Premise deployments");
    }

    [Fact]
    public void IsLicenseValid_WithSaaSDeployment_ShouldReturnTrue()
    {
        // Arrange
        var config = CreateSaaSConfiguration();

        // Act & Assert
        config.IsLicenseValid().Should().BeTrue();
    }

    [Fact]
    public void IsLicenseValid_WithValidOnPremiseLicense_ShouldReturnTrue()
    {
        // Arrange
        var config = CreateOnPremiseConfiguration();

        // Act & Assert
        config.IsLicenseValid().Should().BeTrue();
    }

    [Fact]
    public void IsLicenseValid_WithExpiredOnPremiseLicense_ShouldReturnFalse()
    {
        // Arrange
        var expiredDate = DateTimeOffset.UtcNow.AddDays(-1);
        var config = SystemConfiguration.CreateForOnPremise(
            "Test Org",
            "LICENSE-KEY",
            expiredDate,
            "test@test.com",
            "+1234567890",
            _setupBy);

        // Act & Assert
        config.IsLicenseValid().Should().BeFalse();
    }

    [Fact]
    public void IsLicenseValid_WithEmptyLicenseKey_ShouldReturnFalse()
    {
        // Arrange
        var config = CreateOnPremiseConfiguration();
        config.UpdateLicense("", DateTimeOffset.UtcNow.AddYears(1), _updatedBy);

        // Act & Assert
        config.IsLicenseValid().Should().BeFalse();
    }

    [Theory]
    [InlineData(BusinessFunction.BusinessOnboarding, true)]
    [InlineData(BusinessFunction.UserManagement, true)]
    [InlineData(BusinessFunction.BusinessManagement, true)]
    [InlineData(BusinessFunction.SubscriptionManagement, true)]
    [InlineData(BusinessFunction.FIRSIntegration, true)]
    [InlineData(BusinessFunction.Compliance, true)]
    [InlineData(BusinessFunction.Analytics, true)]
    public void CanKMPGManage_WithSaaSDeployment_ShouldReturnTrueForAllFunctions(BusinessFunction function, bool expected)
    {
        // Arrange
        var config = CreateSaaSConfiguration();

        // Act & Assert
        config.CanKMPGManage(function).Should().Be(expected);
    }

    [Theory]
    [InlineData(BusinessFunction.BusinessOnboarding, false)]
    [InlineData(BusinessFunction.UserManagement, false)]
    [InlineData(BusinessFunction.BusinessManagement, false)]
    [InlineData(BusinessFunction.SubscriptionManagement, true)]
    [InlineData(BusinessFunction.FIRSIntegration, false)]
    [InlineData(BusinessFunction.Compliance, false)]
    [InlineData(BusinessFunction.Analytics, false)]
    public void CanKMPGManage_WithOnPremiseDeployment_ShouldReturnTrueOnlyForSubscriptionManagement(BusinessFunction function, bool expected)
    {
        // Arrange
        var config = CreateOnPremiseConfiguration();

        // Act & Assert
        config.CanKMPGManage(function).Should().Be(expected);
    }

    [Fact]
    public void DeploymentModeEnum_ShouldHaveCorrectValues()
    {
        // Assert
        ((int)DeploymentMode.Cloud).Should().Be(0);
        ((int)DeploymentMode.OnPremise).Should().Be(1);
    }

    [Fact]
    public void BusinessFunctionEnum_ShouldHaveAllExpectedValues()
    {
        // Assert
        Enum.GetNames<BusinessFunction>().Should().Contain([
            "BusinessOnboarding",
            "UserManagement",
            "BusinessManagement",
            "SubscriptionManagement",
            "FIRSIntegration",
            "Compliance",
            "Analytics"
        ]);
    }

    private SystemConfiguration CreateSaaSConfiguration()
    {
        return SystemConfiguration.CreateForSaaS("Aegis Nigeria", _setupBy);
    }

    private SystemConfiguration CreateOnPremiseConfiguration()
    {
        return SystemConfiguration.CreateForOnPremise(
            "Client Corporation",
            "LICENSE-KEY-12345",
            DateTimeOffset.UtcNow.AddYears(1),
            "admin@client.com",
            "+234-800-123-4567",
            _setupBy);
    }
}