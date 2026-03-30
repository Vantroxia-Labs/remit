using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities;

public class BusinessOnboardingTests
{
    private readonly TIN _validTin;
    private readonly Address _validAddress;

    public BusinessOnboardingTests()
    {
        _validTin = TIN.Create("12345678-9012");
        _validAddress = Address.Create("123 Business St", "Lagos", "Lagos", "Nigeria", "100001");
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateBusinessOnboarding()
    {
        // Arrange
        var companyName = "Test Company Ltd";
        var businessRegNumber = "RC123456";
        var contactEmail = "contact@testcompany.com";
        var contactPhone = "+234-800-123-4567";
        var contactPersonName = "John Doe";
        var contactPersonTitle = "CEO";
        var deploymentType = BusinessDeploymentType.SaaS;
        var expectedMonthlyInvoices = 1000;
        var expectedUsers = 10;

        // Act
        var onboarding = BusinessOnboarding.Create(
            companyName,
            businessRegNumber,
            _validTin,
            _validAddress,
            contactEmail,
            contactPhone,
            contactPersonName,
            contactPersonTitle,
            deploymentType,
            expectedMonthlyInvoices,
            expectedUsers);

        // Assert
        onboarding.Should().NotBeNull();
        onboarding.CompanyName.Should().Be(companyName);
        onboarding.BusinessRegistrationNumber.Should().Be(businessRegNumber);
        onboarding.TaxIdentificationNumber.Should().Be(_validTin);
        onboarding.RegisteredAddress.Should().Be(_validAddress);
        onboarding.ContactEmail.Should().Be(contactEmail);
        onboarding.ContactPhone.Should().Be(contactPhone);
        onboarding.ContactPersonName.Should().Be(contactPersonName);
        onboarding.ContactPersonTitle.Should().Be(contactPersonTitle);
        onboarding.DeploymentType.Should().Be(deploymentType);
        onboarding.ExpectedMonthlyInvoices.Should().Be(expectedMonthlyInvoices);
        onboarding.ExpectedUsers.Should().Be(expectedUsers);
        onboarding.Status.Should().Be(BusinessOnboardingStatus.Submitted);
        onboarding.StatusLastChanged.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        onboarding.RiskAssessment.Should().Be(BusinessRiskAssessment.Pending);
        onboarding.ComplianceCheckPassed.Should().BeFalse();
        onboarding.HasFIRSCredentials.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOptionalParameters_ShouldCreateBusinessOnboarding()
    {
        // Arrange
        var specialRequirements = "Custom integration needed";
        var onPremiseDetails = "{ \"infrastructure\": \"Azure\" }";
        var domainWhitelist = "[\"company.com\", \"subsidiary.com\"]";

        // Act
        var onboarding = BusinessOnboarding.Create(
            "Test Company",
            "RC123456",
            _validTin,
            _validAddress,
            "contact@test.com",
            "+1234567890",
            "John Doe",
            "CEO",
            BusinessDeploymentType.OnPremise,
            500,
            5,
            specialRequirements,
            onPremiseDetails,
            domainWhitelist);

        // Assert
        onboarding.SpecialRequirements.Should().Be(specialRequirements);
        onboarding.OnPremiseDetails.Should().Be(onPremiseDetails);
        onboarding.DomainWhitelist.Should().Be(domainWhitelist);
    }

    [Fact]
    public void AssignKMPGReviewer_ShouldUpdateReviewerAndStatus()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var reviewerId = Guid.NewGuid();
        var notes = "Assigned for initial review";

        // Act
        onboarding.AssignKMPGReviewer(reviewerId, notes);

        // Assert
        onboarding.AssignedKMPGReviewer.Should().Be(reviewerId);
        onboarding.ReviewStartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        onboarding.ReviewNotes.Should().Be(notes);
        onboarding.Status.Should().Be(BusinessOnboardingStatus.UnderReview);
        onboarding.StatusReason.Should().Be("Assigned to KMPG reviewer");
    }

    [Fact]
    public void UpdateRiskAssessment_ShouldUpdateRiskLevel()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var notes = "Low risk business - standard processing";

        // Act
        onboarding.UpdateRiskAssessment(BusinessRiskAssessment.Low, notes);

        // Assert
        onboarding.RiskAssessment.Should().Be(BusinessRiskAssessment.Low);
        onboarding.ReviewNotes.Should().Be(notes);
    }

    [Fact]
    public void UpdateRiskAssessment_WithExistingNotes_ShouldAppendNotes()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        onboarding.AssignKMPGReviewer(Guid.NewGuid(), "Initial notes");
        var additionalNotes = "Additional risk assessment notes";

        // Act
        onboarding.UpdateRiskAssessment(BusinessRiskAssessment.Medium, additionalNotes);

        // Assert
        onboarding.RiskAssessment.Should().Be(BusinessRiskAssessment.Medium);
        onboarding.ReviewNotes.Should().Contain("Initial notes");
        onboarding.ReviewNotes.Should().Contain(additionalNotes);
    }

    [Fact]
    public void AddFIRSCredentials_WithAllCredentials_ShouldUpdateFIRSInfo()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var apiKey = "test-api-key";
        var apiSecret = "test-api-secret";
        var serviceId = "TEST-SERVICE-ID";
        var secretKey = "test-secret-key";

        // Act
        onboarding.AddFIRSCredentials(apiKey, apiSecret, serviceId, secretKey);

        // Assert
        onboarding.FIRSApiKey.Should().Be(apiKey);
        onboarding.FIRSApiSecret.Should().Be(apiSecret);
        onboarding.FIRSServiceId.Should().Be(serviceId);
        onboarding.FIRSSecretKey.Should().Be(secretKey);
        onboarding.HasFIRSCredentials.Should().BeTrue();
    }

    [Fact]
    public void AddFIRSCredentials_WithPartialCredentials_ShouldStillMarkAsHavingCredentials()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();

        // Act
        onboarding.AddFIRSCredentials(apiKey: "test-api-key");

        // Assert
        onboarding.FIRSApiKey.Should().Be("test-api-key");
        onboarding.HasFIRSCredentials.Should().BeTrue();
    }

    [Fact]
    public void CompleteReview_ShouldUpdateStatusAndTime()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        onboarding.AssignKMPGReviewer(Guid.NewGuid());
        var reviewNotes = "Review completed successfully";

        // Act
        onboarding.CompleteReview(reviewNotes);

        // Assert
        onboarding.ReviewCompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        onboarding.Status.Should().Be(BusinessOnboardingStatus.ReviewCompleted);
        onboarding.ReviewNotes.Should().Contain(reviewNotes);
    }

    [Fact]
    public void ApproveOnboarding_WithCompletedReview_ShouldApprove()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        onboarding.AssignKMPGReviewer(Guid.NewGuid());
        onboarding.CompleteReview();
        var approvedBy = Guid.NewGuid();
        var approvalNotes = "Approved for onboarding";

        // Act
        onboarding.ApproveOnboarding(approvedBy, approvalNotes);

        // Assert
        onboarding.ApprovedBy.Should().Be(approvedBy);
        onboarding.ApprovedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        onboarding.ApprovalNotes.Should().Be(approvalNotes);
        onboarding.Status.Should().Be(BusinessOnboardingStatus.Approved);
    }

    [Fact]
    public void ApproveOnboarding_WithoutCompletedReview_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var approvedBy = Guid.NewGuid();

        // Act & Assert
        var action = () => onboarding.ApproveOnboarding(approvedBy);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot approve onboarding before review is completed");
    }

    [Fact]
    public void RejectOnboarding_ShouldUpdateRejectionInfo()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var rejectedBy = Guid.NewGuid();
        var rejectionReason = "Insufficient documentation provided";

        // Act
        onboarding.RejectOnboarding(rejectedBy, rejectionReason);

        // Assert
        onboarding.RejectedBy.Should().Be(rejectedBy);
        onboarding.RejectedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        onboarding.RejectionReason.Should().Be(rejectionReason);
        onboarding.Status.Should().Be(BusinessOnboardingStatus.Rejected);
    }

    [Fact]
    public void MarkBusinessCreated_ShouldUpdateBusinessInfo()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var businessId = Guid.NewGuid();

        // Act
        onboarding.MarkBusinessCreated(businessId);

        // Assert
        onboarding.CreatedBusinessId.Should().Be(businessId);
        onboarding.BusinessCreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        onboarding.Status.Should().Be(BusinessOnboardingStatus.Completed);
    }

    [Fact]
    public void UpdateComplianceStatus_ShouldUpdateComplianceInfo()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var notes = "All compliance checks passed";

        // Act
        onboarding.UpdateComplianceStatus(true, notes);

        // Assert
        onboarding.ComplianceCheckPassed.Should().BeTrue();
        onboarding.ComplianceNotes.Should().Be(notes);
    }

    [Fact]
    public void AddDocument_ShouldAddToDocumentsList()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var documentRef = "doc-12345.pdf";

        // Act
        onboarding.AddDocument(documentRef);

        // Assert
        onboarding.UploadedDocuments.Should().NotBeNullOrEmpty();
        onboarding.UploadedDocuments.Should().Contain(documentRef);
    }

    [Fact]
    public void RequestAdditionalInformation_ShouldUpdateStatus()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        var requestDetails = "Please provide additional tax certificates";

        // Act
        onboarding.RequestAdditionalInformation(requestDetails);

        // Assert
        onboarding.Status.Should().Be(BusinessOnboardingStatus.PendingInformation);
        onboarding.StatusReason.Should().Be(requestDetails);
    }

    [Fact]
    public void CanBeApproved_WithAllRequirements_ShouldReturnTrue()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        onboarding.AssignKMPGReviewer(Guid.NewGuid());
        onboarding.CompleteReview();
        onboarding.UpdateComplianceStatus(true);
        onboarding.UpdateRiskAssessment(BusinessRiskAssessment.Low);

        // Act & Assert
        onboarding.CanBeApproved().Should().BeTrue();
    }

    [Fact]
    public void CanBeApproved_WithHighRisk_ShouldReturnFalse()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        onboarding.AssignKMPGReviewer(Guid.NewGuid());
        onboarding.CompleteReview();
        onboarding.UpdateComplianceStatus(true);
        onboarding.UpdateRiskAssessment(BusinessRiskAssessment.High);

        // Act & Assert
        onboarding.CanBeApproved().Should().BeFalse();
    }

    [Fact]
    public void RequiresAdditionalInformation_WithPendingStatus_ShouldReturnTrue()
    {
        // Arrange
        var onboarding = CreateTestBusinessOnboarding();
        onboarding.RequestAdditionalInformation("Need more info");

        // Act & Assert
        onboarding.RequiresAdditionalInformation().Should().BeTrue();
    }

    [Theory]
    [InlineData(BusinessDeploymentType.SaaS)]
    [InlineData(BusinessDeploymentType.OnPremise)]
    public void Create_WithDifferentDeploymentTypes_ShouldWork(BusinessDeploymentType deploymentType)
    {
        // Act
        var onboarding = BusinessOnboarding.Create(
            "Test Company",
            "RC123456",
            _validTin,
            _validAddress,
            "contact@test.com",
            "+1234567890",
            "John Doe",
            "CEO",
            deploymentType,
            100,
            5);

        // Assert
        onboarding.DeploymentType.Should().Be(deploymentType);
    }

    [Theory]
    [InlineData(BusinessOnboardingStatus.Submitted)]
    [InlineData(BusinessOnboardingStatus.UnderReview)]
    [InlineData(BusinessOnboardingStatus.PendingInformation)]
    [InlineData(BusinessOnboardingStatus.ReviewCompleted)]
    [InlineData(BusinessOnboardingStatus.Approved)]
    [InlineData(BusinessOnboardingStatus.Rejected)]
    [InlineData(BusinessOnboardingStatus.Completed)]
    [InlineData(BusinessOnboardingStatus.Cancelled)]
    public void BusinessOnboardingStatus_ShouldHaveAllExpectedValues(BusinessOnboardingStatus expectedStatus)
    {
        // Act & Assert
        Enum.IsDefined(typeof(BusinessOnboardingStatus), expectedStatus).Should().BeTrue();
    }

    [Theory]
    [InlineData(BusinessRiskAssessment.Pending)]
    [InlineData(BusinessRiskAssessment.Low)]
    [InlineData(BusinessRiskAssessment.Medium)]
    [InlineData(BusinessRiskAssessment.High)]
    public void BusinessRiskAssessment_ShouldHaveAllExpectedValues(BusinessRiskAssessment expectedAssessment)
    {
        // Act & Assert
        Enum.IsDefined(typeof(BusinessRiskAssessment), expectedAssessment).Should().BeTrue();
    }

    private BusinessOnboarding CreateTestBusinessOnboarding()
    {
        return BusinessOnboarding.Create(
            "Test Company Ltd",
            "RC123456",
            _validTin,
            _validAddress,
            "contact@testcompany.com",
            "+234-800-123-4567",
            "John Doe",
            "CEO",
            BusinessDeploymentType.SaaS,
            1000,
            10);
    }
}