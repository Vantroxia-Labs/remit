using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.BusinessManagement;

/// <summary>
/// Comprehensive tests for Business entity targeting 100% code coverage
/// </summary>
public class BusinessTests
{
    private readonly Guid _adminUserId = Guid.NewGuid();
    private readonly Guid _createdBy = Guid.NewGuid();
    private readonly Guid _updatedBy = Guid.NewGuid();
    private readonly Guid _firsBusinessId = Guid.NewGuid();
    private readonly TIN _validTin;
    private readonly Address _validAddress;

    public BusinessTests()
    {
        _validTin = TIN.Create("12345678-9012");
        _validAddress = Address.Create("123 Business St", "Lagos", "Lagos", "Nigeria", "100001");
    }

    #region Constructor Tests

    [Fact]
    public void Create_WithValidParameters_ShouldCreateBusiness()
    {
        // Act
        var business = Business.Create(
            "Test Business Ltd",
            "A test business for unit testing",
            "RC123456",
            _validTin,
            _validAddress,
            "TST",
            "contact@testbusiness.com",
            _adminUserId,
            _createdBy,
            "+234-800-123-4567",
            "SERV1234",
            "Technology",
            _firsBusinessId);

        // Assert
        business.Should().NotBeNull();
        business.Name.Should().Be("Test Business Ltd");
        business.Description.Should().Be("A test business for unit testing");
        business.BusinessRegistrationNumber.Should().Be("RC123456");
        business.TaxIdentificationNumber.Should().Be(_validTin);
        business.RegisteredAddress.Should().Be(_validAddress);
        business.InvoicePrefix.Should().Be("TST");
        business.ContactEmail.Should().Be("contact@testbusiness.com");
        business.ContactPhone.Should().Be("+234-800-123-4567");
        business.AdminUserId.Should().Be(_adminUserId);
        business.ServiceId.Should().Be("SERV1234");
        business.Industry.Should().Be("Technology");
        business.FIRSBusinessId.Should().Be(_firsBusinessId);
        business.Status.Should().Be(BusinessStatus.Active);
        business.CreatedBy.Should().Be(_createdBy);
        business.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("", "Description", "TST", "RC123456", "contact@test.com")]
    [InlineData(null, "Description", "TST", "RC123456", "contact@test.com")]
    [InlineData("Name", "", "TST", "RC123456", "contact@test.com")]
    [InlineData("Name", null, "TST", "RC123456", "contact@test.com")]
    [InlineData("Name", "Description", "", "RC123456", "contact@test.com")]
    [InlineData("Name", "Description", null, "RC123456", "contact@test.com")]
    [InlineData("Name", "Description", "TST", "", "contact@test.com")]
    [InlineData("Name", "Description", "TST", null, "contact@test.com")]
    [InlineData("Name", "Description", "TST", "RC123456", "")]
    [InlineData("Name", "Description", "TST", "RC123456", null)]
    [InlineData("Name", "Description", "TST", "RC123456", "invalid-email")]
    public void Create_WithInvalidParameters_ShouldThrowBadRequestException(
        string? name, string? description, string? invoicePrefix, string? businessRegistrationNumber, string? contactEmail)
    {
        // Act & Assert
        var action = () => Business.Create(
            name!,
            description!,
            businessRegistrationNumber!,
            _validTin,
            _validAddress,
            invoicePrefix!,
            contactEmail!,
            _adminUserId,
            _createdBy,
            "+234-800-123-4567",
            "SERV1234",
            "Technology",
            _firsBusinessId);

        action.Should().Throw<BadRequestException>();
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrowBadRequestException()
    {
        // Arrange
        var longName = new string('A', 201); // 201 characters

        // Act & Assert
        var action = () => Business.Create(
            longName,
            "Valid description",
            "RC123456",
            _validTin,
            _validAddress,
            "TST",
            "contact@test.com",
            _adminUserId,
            _createdBy,
            "+234-800-123-4567",
            "SERV1234",
            "Technology",
            _firsBusinessId);

        action.Should().Throw<BadRequestException>()
            .WithMessage("Business name cannot exceed 200 characters*");
    }

    [Fact]
    public void Create_WithTooLongDescription_ShouldThrowBadRequestException()
    {
        // Arrange
        var longDescription = new string('A', 501); // 501 characters

        // Act & Assert
        var action = () => Business.Create(
            "Valid Name",
            longDescription,
            "RC123456",
            _validTin,
            _validAddress,
            "TST",
            "contact@test.com",
            _adminUserId,
            _createdBy,
            "+234-800-123-4567",
            "SERV1234",
            "Technology",
            _firsBusinessId);

        action.Should().Throw<BadRequestException>()
            .WithMessage("Business description cannot exceed 500 characters*");
    }

    #endregion

    #region Status Management Tests

    [Fact]
    public void Activate_ShouldSetStatusToActive()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.Deactivate(_updatedBy);

        // Act
        business.Activate(_updatedBy);

        // Assert
        business.Status.Should().Be(BusinessStatus.Active);
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Deactivate_ShouldSetStatusToInactive()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act
        business.Deactivate(_updatedBy);

        // Assert
        business.Status.Should().Be(BusinessStatus.Inactive);
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateBusiness()
    {
        // Arrange
        var business = CreateTestBusiness();
        var newAddress = Address.Create("456 New St", "Abuja", "FCT", "Nigeria", "900001");

        // Act
        business.Update(
            "Updated description",
            "UPD",
            "newemail@test.com",
            newAddress,
            _updatedBy,
            "+234-800-999-8888");

        // Assert
        business.Description.Should().Be("Updated description");
        business.InvoicePrefix.Should().Be("UPD");
        business.ContactEmail.Should().Be("newemail@test.com");
        business.RegisteredAddress.Should().Be(newAddress);
        business.ContactPhone.Should().Be("+234-800-999-8888");
    }

    [Fact]
    public void Update_WithoutOptionalPhone_ShouldUpdateBusinessWithEmptyPhone()
    {
        // Arrange
        var business = CreateTestBusiness();
        var newAddress = Address.Create("456 New St", "Abuja", "FCT", "Nigeria", "900001");

        // Act
        business.Update(
            "Updated description",
            "UPD",
            "newemail@test.com",
            newAddress,
            _updatedBy);

        // Assert
        business.ContactPhone.Should().Be("");
    }

    [Theory]
    [InlineData("", "UPD", "email@test.com")]
    [InlineData(null, "UPD", "email@test.com")]
    [InlineData("Description", "", "email@test.com")]
    [InlineData("Description", null, "email@test.com")]
    [InlineData("Description", "UPD", "")]
    [InlineData("Description", "UPD", null)]
    [InlineData("Description", "UPD", "invalid-email")]
    public void Update_WithInvalidParameters_ShouldThrowBadRequestException(
        string? description, string? invoicePrefix, string? contactEmail)
    {
        // Arrange
        var business = CreateTestBusiness();
        var newAddress = Address.Create("456 New St", "Abuja", "FCT", "Nigeria", "900001");

        // Act & Assert
        var action = () => business.Update(description!, invoicePrefix!, contactEmail!, newAddress, _updatedBy);
        action.Should().Throw<BadRequestException>();
    }

    #endregion

    #region Admin Management Tests

    [Fact]
    public void ChangeAdmin_WithDifferentAdmin_ShouldChangeAdmin()
    {
        // Arrange
        var business = CreateTestBusiness();
        var newAdminUserId = Guid.NewGuid();

        // Act
        business.ChangeAdmin(newAdminUserId, _updatedBy);

        // Assert
        business.AdminUserId.Should().Be(newAdminUserId);
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ChangeAdmin_WithSameAdmin_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        var action = () => business.ChangeAdmin(_adminUserId, _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("New admin is the same as current admin");
    }

    [Fact]
    public void SetAdmin_WithNewAdmin_ShouldSetAdmin()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.GetType().GetProperty("AdminUserId")?.SetValue(business, null);
        var newAdminUserId = Guid.NewGuid();

        // Act
        business.SetAdmin(newAdminUserId, _updatedBy);

        // Assert
        business.AdminUserId.Should().Be(newAdminUserId);
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.Now, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetAdmin_WithExistingAdmin_ShouldThrowConflictException()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        var action = () => business.SetAdmin(_adminUserId, _updatedBy);
        action.Should().Throw<ConflictException>()
            .WithMessage("User is already the admin of this business");
    }

    #endregion

    #region Access Control Tests

    [Fact]
    public void IsAdminUser_WithAdminUserId_ShouldReturnTrue()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        business.IsAdminUser(_adminUserId).Should().BeTrue();
    }

    [Fact]
    public void IsAdminUser_WithDifferentUserId_ShouldReturnFalse()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        business.IsAdminUser(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsOwner_ShouldReturnSameAsIsAdminUser()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        business.IsOwner(_adminUserId).Should().Be(business.IsAdminUser(_adminUserId));
        business.IsOwner(Guid.NewGuid()).Should().Be(business.IsAdminUser(Guid.NewGuid()));
    }

    [Fact]
    public void IsUserInBusiness_WithUserInBusiness_ShouldReturnTrue()
    {
        // Arrange
        var business = CreateTestBusiness();
        var userId = Guid.NewGuid();

        // Use reflection to add user to private _users collection
        var usersField = business.GetType().GetField("_users", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var usersList = (List<User>)usersField!.GetValue(business)!;
        var mockUser = CreateMockUser(userId);
        usersList.Add(mockUser);

        // Act & Assert
        business.IsUserInBusiness(userId).Should().BeTrue();
    }

    [Fact]
    public void IsUserInBusiness_WithUserNotInBusiness_ShouldReturnFalse()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        business.IsUserInBusiness(Guid.NewGuid()).Should().BeFalse();
    }

    #endregion

    #region Flow Rule Tests

    [Fact]
    public void AssignFlowRule_WithValidFlowRuleId_ShouldAssignFlowRule()
    {
        // Arrange
        var business = CreateTestBusiness();
        var flowRuleId = Guid.NewGuid();

        // Act
        business.AssignFlowRule(flowRuleId, _updatedBy);

        // Assert
        business.FlowRuleId.Should().Be(flowRuleId);
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void AssignFlowRule_WithEmptyGuid_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        var action = () => business.AssignFlowRule(Guid.Empty, _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid FlowRule ID");
    }

    [Fact]
    public void GetActiveFlowRules_ShouldReturnOnlyActiveFlowRules()
    {
        // Arrange
        var business = CreateTestBusiness();
        var activeFlowRule = CreateMockFlowRule(false);
        var deletedFlowRule = CreateMockFlowRule(true);

        var flowRulesField = business.GetType().GetField("_flowRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var flowRulesList = (List<FlowRule>)flowRulesField!.GetValue(business)!;
        flowRulesList.Add(activeFlowRule);
        flowRulesList.Add(deletedFlowRule);

        // Act
        var activeFlowRules = business.GetActiveFlowRules().ToList();

        // Assert
        activeFlowRules.Should().HaveCount(1);
        activeFlowRules.Should().Contain(activeFlowRule);
        activeFlowRules.Should().NotContain(deletedFlowRule);
    }

    [Fact]
    public void GetApplicableFlowRules_ShouldReturnFlowRulesWithAmountLessOrEqual()
    {
        // Arrange
        var business = CreateTestBusiness();
        var flowRule1 = CreateMockFlowRule(false, 100.0);
        var flowRule2 = CreateMockFlowRule(false, 200.0);
        var flowRule3 = CreateMockFlowRule(false, 300.0);

        var flowRulesField = business.GetType().GetField("_flowRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var flowRulesList = (List<FlowRule>)flowRulesField!.GetValue(business)!;
        flowRulesList.AddRange([flowRule1, flowRule2, flowRule3]);

        // Act
        var applicableFlowRules = business.GetApplicableFlowRules(250.0).ToList();

        // Assert
        applicableFlowRules.Should().HaveCount(2);
        applicableFlowRules.Should().Contain(flowRule1);
        applicableFlowRules.Should().Contain(flowRule2);
        applicableFlowRules.Should().NotContain(flowRule3);
    }

    [Fact]
    public void GetFlowRuleCount_ShouldReturnCountOfActiveFlowRules()
    {
        // Arrange
        var business = CreateTestBusiness();
        var activeFlowRule1 = CreateMockFlowRule(false);
        var activeFlowRule2 = CreateMockFlowRule(false);
        var deletedFlowRule = CreateMockFlowRule(true);

        var flowRulesField = business.GetType().GetField("_flowRules", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var flowRulesList = (List<FlowRule>)flowRulesField!.GetValue(business)!;
        flowRulesList.AddRange([activeFlowRule1, activeFlowRule2, deletedFlowRule]);

        // Act
        var count = business.GetFlowRuleCount();

        // Assert
        count.Should().Be(2);
    }

    #endregion

    #region Subscription Tests

    [Fact]
    public void HasActiveSubscription_WithActiveSubscription_ShouldReturnTrue()
    {
        // Arrange
        var business = CreateTestBusiness();
        var mockSubscription = CreateMockSubscription(true);
        var subscriptionsField = business.GetType().GetField("_subscriptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var subscriptionsList = (List<AegisEInvoicing.Domain.Entities.BusinessManagement.Subscription>)subscriptionsField!.GetValue(business)!;
        subscriptionsList.Add(mockSubscription);

        // Act & Assert
        business.HasActiveSubscription().Should().BeTrue();
    }

    [Fact]
    public void HasActiveSubscription_WithoutSubscription_ShouldReturnFalse()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        business.HasActiveSubscription().Should().BeFalse();
    }

    [Fact]
    public void ValidateSubscriptionAccess_WithActiveSubscription_ShouldNotThrow()
    {
        // Arrange
        var business = CreateTestBusiness();
        var mockSubscription = CreateMockSubscription(true);
        var subscriptionsField = business.GetType().GetField("_subscriptions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var subscriptionsList = (List<AegisEInvoicing.Domain.Entities.BusinessManagement.Subscription>)subscriptionsField!.GetValue(business)!;
        subscriptionsList.Add(mockSubscription);

        // Act & Assert
        var action = () => business.ValidateSubscriptionAccess();
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateSubscriptionAccess_WithoutActiveSubscription_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        var action = () => business.ValidateSubscriptionAccess();
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Business subscription is not active. Please contact support or renew your subscription.");
    }

    #endregion

    #region API Key Management Tests

    [Fact]
    public void SetApiKey_WithValidApiKey_ShouldSetApiKey()
    {
        // Arrange
        var business = CreateTestBusiness();
        var apiKey = "test-api-key-12345";

        // Act
        business.SetApiKey(apiKey, _updatedBy);

        // Assert
        business.ApiKey.Should().Be(apiKey);
        business.ApiKeyGeneratedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        business.IsApiKeyActive.Should().BeTrue();
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SetApiKey_WithInvalidApiKey_ShouldThrowBadRequestException(string? apiKey)
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        var action = () => business.SetApiKey(apiKey, _updatedBy);
        action.Should().Throw<BadRequestException>()
            .WithMessage("API key cannot be null or empty*");
    }

    [Fact]
    public void SetApiKey_WithExistingActiveApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.SetApiKey("existing-key", _updatedBy);

        // Act & Assert
        var action = () => business.SetApiKey("new-key", _updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Business already has an active API key");
    }

    [Fact]
    public void RevokeApiKey_WithActiveApiKey_ShouldRevokeApiKey()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.SetApiKey("test-api-key", _updatedBy);

        // Act
        business.RevokeApiKey(_updatedBy);

        // Assert
        business.ApiKey.Should().BeNull();
        business.IsApiKeyActive.Should().BeFalse();
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void RevokeApiKey_WithoutActiveApiKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        var action = () => business.RevokeApiKey(_updatedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("No active API key to revoke");
    }

    [Fact]
    public void RecordApiKeyUsage_ShouldUpdateLastUsedAt()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act
        business.RecordApiKeyUsage();

        // Assert
        business.ApiKeyLastUsedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void HasValidApiKey_WithValidActiveApiKey_ShouldReturnTrue()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.SetApiKey("valid-api-key", _updatedBy);

        // Act & Assert
        business.HasValidApiKey().Should().BeTrue();
    }

    [Fact]
    public void HasValidApiKey_WithoutApiKey_ShouldReturnFalse()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        business.HasValidApiKey().Should().BeFalse();
    }

    [Fact]
    public void HasValidApiKey_WithInactiveApiKey_ShouldReturnFalse()
    {
        // Arrange
        var business = CreateTestBusiness();
        business.SetApiKey("test-key", _updatedBy);
        business.RevokeApiKey(_updatedBy);

        // Act & Assert
        business.HasValidApiKey().Should().BeFalse();
    }

    #endregion

    #region FIRS Service Tests

    [Fact]
    public void HasFIRSServiceId_WithValidServiceId_ShouldReturnTrue()
    {
        // Arrange
        var business = CreateTestBusiness();

        // Act & Assert
        business.HasFIRSServiceId().Should().BeTrue();
    }

    [Fact]
    public void HasFIRSServiceId_WithInvalidServiceId_ShouldReturnFalse()
    {
        // Arrange
        var business = CreateBusinessWithServiceId("SHORT");

        // Act & Assert
        business.HasFIRSServiceId().Should().BeFalse();
    }

    [Fact]
    public void HasFIRSServiceId_WithEmptyServiceId_ShouldReturnFalse()
    {
        // Arrange
        var business = CreateBusinessWithServiceId("");

        // Act & Assert
        business.HasFIRSServiceId().Should().BeFalse();
    }

    [Fact]
    public void AssignFirsApiConfiguration_ShouldAssignConfiguration()
    {
        // Arrange
        var business = CreateTestBusiness();
        var configurationId = Guid.NewGuid();

        // Act
        business.AssignFirsApiConfiguration(configurationId, _updatedBy);

        // Assert
        business.BusinessFIRSApiConfigurationId.Should().Be(configurationId);
        business.UpdatedBy.Should().Be(_updatedBy);
        business.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    #endregion

    #region Helper Methods

    private Business CreateTestBusiness()
    {
        return Business.Create(
            "Test Business Ltd",
            "A test business for unit testing",
            "RC123456",
            _validTin,
            _validAddress,
            "TST",
            "contact@testbusiness.com",
            _adminUserId,
            _createdBy,
            "+234-800-123-4567",
            "SERV1234",
            "Technology",
            _firsBusinessId);
    }

    private Business CreateBusinessWithServiceId(string serviceId)
    {
        return Business.Create(
            "Test Business Ltd",
            "A test business for unit testing",
            "RC123456",
            _validTin,
            _validAddress,
            "TST",
            "contact@testbusiness.com",
            _adminUserId,
            _createdBy,
            "+234-800-123-4567",
            serviceId,
            "Technology",
            _firsBusinessId);
    }

    private User CreateMockUser(Guid userId)
    {
        // Create a mock user using reflection since User constructor is private
        var user = (User)Activator.CreateInstance(typeof(User), true)!;
        user.GetType().GetProperty("Id")?.SetValue(user, userId);
        return user;
    }

    private FlowRule CreateMockFlowRule(bool isDeleted, double amount = 100.0)
    {
        var flowRule = (FlowRule)Activator.CreateInstance(typeof(FlowRule), true)!;
        flowRule.GetType().GetProperty("IsDeleted")?.SetValue(flowRule, isDeleted);
        flowRule.GetType().GetProperty("Amount")?.SetValue(flowRule, amount);
        flowRule.GetType().GetProperty("CreatedAt")?.SetValue(flowRule, DateTimeOffset.UtcNow);
        return flowRule;
    }

    private Subscription CreateMockSubscription(bool isActive)
    {
        var subscription = (Subscription)Activator.CreateInstance(typeof(Subscription), true)!;

        // Set Status to Active or Cancelled based on isActive parameter
        var statusProperty = subscription.GetType().GetProperty("Status", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        statusProperty?.SetValue(subscription, isActive ? SubscriptionStatus.Active : SubscriptionStatus.Cancelled);

        // Set EndDate to future (active) or past (expired) based on isActive
        var endDateProperty = subscription.GetType().GetProperty("EndDate", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        endDateProperty?.SetValue(subscription, isActive ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddDays(-30));

        return subscription;
    }

    #endregion
}