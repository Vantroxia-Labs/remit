using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.ValueObjects;

namespace AegisEInvoicing.Domain.Entities;

/// <summary>
/// Represents the onboarding process for businesses by Aegis.
/// Tracks the entire lifecycle from initial request to active business.
/// </summary>
public class BusinessOnboarding : AuditableEntity
{
    public string CompanyName { get; private set; } = default!;
    public string BusinessRegistrationNumber { get; private set; } = default!;
    public TIN TaxIdentificationNumber { get; private set; } = default!;
    public Address RegisteredAddress { get; private set; } = default!;
    public string ContactEmail { get; private set; } = default!;
    public string ContactPhone { get; private set; } = default!;
    public string ContactPersonName { get; private set; } = default!;
    public string ContactPersonTitle { get; private set; } = default!;
    
    // Deployment Information
    public BusinessDeploymentType DeploymentType { get; private set; }
    public string? OnPremiseDetails { get; private set; } // JSON with infrastructure details
    public string? DomainWhitelist { get; private set; } // JSON array for On-Premise
    
    // FIRS Integration Details
    public string? FIRSApiKey { get; private set; }
    public string? FIRSApiSecret { get; private set; }
    public string? FIRSServiceId { get; private set; }
    public string? FIRSSecretKey { get; private set; }
    public bool HasFIRSCredentials { get; private set; }
    
    // Business Requirements
    public int ExpectedMonthlyInvoices { get; private set; }
    public int ExpectedUsers { get; private set; }
    public string? SpecialRequirements { get; private set; }
    
    // Onboarding Status
    public BusinessOnboardingStatus Status { get; private set; }
    public string? StatusReason { get; private set; }
    public DateTimeOffset? StatusLastChanged { get; private set; }
    
    // Aegis Review Process
    public Guid? AssignedKMPGReviewer { get; private set; }
    public DateTimeOffset? ReviewStartedAt { get; private set; }
    public DateTimeOffset? ReviewCompletedAt { get; private set; }
    public string? ReviewNotes { get; private set; }
    public BusinessRiskAssessment RiskAssessment { get; private set; }
    
    // Approval Process
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public string? ApprovalNotes { get; private set; }
    public Guid? RejectedBy { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    
    // Business Creation
    public Guid? CreatedBusinessId { get; private set; }
    public DateTimeOffset? BusinessCreatedAt { get; private set; }
    
    // Documents and Compliance
    public string? UploadedDocuments { get; private set; } // JSON array of document references
    public bool ComplianceCheckPassed { get; private set; }
    public string? ComplianceNotes { get; private set; }
    
    private BusinessOnboarding() { } // EF Constructor

    public static BusinessOnboarding Create(
        string companyName,
        string businessRegistrationNumber,
        TIN taxIdentificationNumber,
        Address registeredAddress,
        string contactEmail,
        string contactPhone,
        string contactPersonName,
        string contactPersonTitle,
        BusinessDeploymentType deploymentType,
        int expectedMonthlyInvoices,
        int expectedUsers,
        string? specialRequirements = null,
        string? onPremiseDetails = null,
        string? domainWhitelist = null)
    {
        var onboarding = new BusinessOnboarding
        {
            CompanyName = companyName,
            BusinessRegistrationNumber = businessRegistrationNumber,
            TaxIdentificationNumber = taxIdentificationNumber,
            RegisteredAddress = registeredAddress,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            ContactPersonName = contactPersonName,
            ContactPersonTitle = contactPersonTitle,
            DeploymentType = deploymentType,
            ExpectedMonthlyInvoices = expectedMonthlyInvoices,
            ExpectedUsers = expectedUsers,
            SpecialRequirements = specialRequirements,
            OnPremiseDetails = onPremiseDetails,
            DomainWhitelist = domainWhitelist,
            Status = BusinessOnboardingStatus.Submitted,
            StatusLastChanged = DateTimeOffset.UtcNow,
            RiskAssessment = BusinessRiskAssessment.Pending,
            ComplianceCheckPassed = false
        };

        return onboarding;
    }

    public void AssignKMPGReviewer(Guid reviewerId, string? notes = null)
    {
        AssignedKMPGReviewer = reviewerId;
        ReviewStartedAt = DateTimeOffset.UtcNow;
        ReviewNotes = notes;
        UpdateStatus(BusinessOnboardingStatus.UnderReview, "Assigned to KMPG reviewer");
    }

    public void UpdateRiskAssessment(BusinessRiskAssessment assessment, string? notes = null)
    {
        RiskAssessment = assessment;
        if (!string.IsNullOrEmpty(notes))
        {
            ReviewNotes = string.IsNullOrEmpty(ReviewNotes) ? notes : $"{ReviewNotes}\n{notes}";
        }
    }

    public void AddFIRSCredentials(
        string? apiKey = null,
        string? apiSecret = null,
        string? serviceId = null,
        string? secretKey = null)
    {
        FIRSApiKey = apiKey;
        FIRSApiSecret = apiSecret;
        FIRSServiceId = serviceId;
        FIRSSecretKey = secretKey;
        HasFIRSCredentials = !string.IsNullOrEmpty(apiKey) || !string.IsNullOrEmpty(serviceId);
    }

    public void CompleteReview(string? reviewNotes = null)
    {
        ReviewCompletedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrEmpty(reviewNotes))
        {
            ReviewNotes = string.IsNullOrEmpty(ReviewNotes) ? reviewNotes : $"{ReviewNotes}\n{reviewNotes}";
        }
        UpdateStatus(BusinessOnboardingStatus.ReviewCompleted, "KMPG review completed");
    }

    public void ApproveOnboarding(Guid approvedBy, string? approvalNotes = null)
    {
        if (Status != BusinessOnboardingStatus.ReviewCompleted)
        {
            throw new InvalidOperationException("Cannot approve onboarding before review is completed");
        }

        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
        ApprovalNotes = approvalNotes;
        UpdateStatus(BusinessOnboardingStatus.Approved, "Onboarding approved by KMPG");
    }

    public void RejectOnboarding(Guid rejectedBy, string rejectionReason)
    {
        RejectedBy = rejectedBy;
        RejectedAt = DateTimeOffset.UtcNow;
        RejectionReason = rejectionReason;
        UpdateStatus(BusinessOnboardingStatus.Rejected, rejectionReason);
    }

    public void MarkBusinessCreated(Guid businessId)
    {
        CreatedBusinessId = businessId;
        BusinessCreatedAt = DateTimeOffset.UtcNow;
        UpdateStatus(BusinessOnboardingStatus.Completed, "Business successfully created and onboarded");
    }

    public void UpdateComplianceStatus(bool passed, string? notes = null)
    {
        ComplianceCheckPassed = passed;
        ComplianceNotes = notes;
    }

    public void AddDocument(string documentReference)
    {
        var documents = string.IsNullOrEmpty(UploadedDocuments) 
            ? new List<string>() 
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(UploadedDocuments) ?? new List<string>();
        
        documents.Add(documentReference);
        UploadedDocuments = System.Text.Json.JsonSerializer.Serialize(documents);
    }

    public void RequestAdditionalInformation(string requestDetails)
    {
        UpdateStatus(BusinessOnboardingStatus.PendingInformation, requestDetails);
    }

    private void UpdateStatus(BusinessOnboardingStatus newStatus, string? reason = null)
    {
        Status = newStatus;
        StatusReason = reason;
        StatusLastChanged = DateTimeOffset.UtcNow;
    }

    public bool CanBeApproved()
    {
        return Status == BusinessOnboardingStatus.ReviewCompleted &&
               ComplianceCheckPassed &&
               RiskAssessment != BusinessRiskAssessment.High &&
               (DeploymentType == BusinessDeploymentType.SaaS || HasFIRSCredentials);
    }

    public bool RequiresAdditionalInformation()
    {
        return Status == BusinessOnboardingStatus.PendingInformation;
    }
}

/// <summary>
/// Business deployment types for onboarding
/// </summary>
public enum BusinessDeploymentType
{
    SaaS = 1,
    OnPremise = 2
}

/// <summary>
/// Onboarding process status
/// </summary>
public enum BusinessOnboardingStatus
{
    Submitted = 1,
    UnderReview = 2,
    PendingInformation = 3,
    ReviewCompleted = 4,
    Approved = 5,
    Rejected = 6,
    Completed = 7,
    Cancelled = 8
}

/// <summary>
/// Risk assessment levels for business onboarding
/// </summary>
public enum BusinessRiskAssessment
{
    Pending = 0,
    Low = 1,
    Medium = 2,
    High = 3
}