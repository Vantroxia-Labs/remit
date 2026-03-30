using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AegisEInvoicing.Domain.Entities.BusinessManagement;

public class Business : AuditableAggregateRoot
{
    // Basic Business Information
    public string Name { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string BusinessRegistrationNumber { get; private set; } = null!;
    public string InvoicePrefix { get; set; } = null!;
    public TIN TaxIdentificationNumber { get; private set; } = null!;
    public Address RegisteredAddress { get; private set; } = null!;

    [EmailAddress]
    public string ContactEmail { get; private set; } = null!;
    public string ContactPhone { get; private set; } = null!;
    public BusinessStatus Status { get; private set; } = BusinessStatus.Active;
    public string Industry { get; private set; } = null!;

    public List<BusinessItem> BusinessItems { get; private set; } = [];
    public List<ItemCategory> ItemCategories { get; private set; } = [];
    public List<Party> Parties { get; private set; } = [];
    public List<Invoice> Invoices { get; private set; } = [];

    public string? FIRSApiKey { get; private set; }
    public string? FIRSClientSecret { get; private set; }


    // User Management
    public Guid? AdminUserId { get; private set; }

    //Subscription Management
    public Guid? SubscriptionId { get; private set; }

    public Guid? BusinessFIRSApiConfigurationId { get; private set; }

    //FlowRule
    public Guid? FlowRuleId { get; private set; }

    // FIRS Integration
    public Guid FIRSBusinessId { get; private set; }
    public string ServiceId { get; private set; } = null!; // FIRS-assigned 8-character ID for IRN generation    
    public string? OAuth2Token { get; private set; }
    public DateTimeOffset? TokenExpiresAt { get; private set; }
    
    // API Access
    public string? ApiKey { get; private set; } // API key for SaaS API access
    public DateTimeOffset? ApiKeyGeneratedAt { get; private set; }
    public DateTimeOffset? ApiKeyLastUsedAt { get; private set; }
    public bool IsApiKeyActive { get; private set; } = false;

    // Navigation properties  
    public User? AdminUser { get; private set; }
    public Subscription? Subscription { get; private set; }
    public BusinessFIRSApiConfiguration? BusinessFIRSApiConfiguration { get; private set; }

    public string? PublicKey { get; private set; }
    public string? Certificate {  get; private set; }

    // Licensing (On-Premise Deployments)
    public DeploymentMode DeploymentMode { get; private set; } = DeploymentMode.Cloud; // Default to Cloud (Aegis-hosted)
    public string? LicenseKey { get; private set; }
    public DateTime? LicenseKeyIssuedDate { get; private set; }
    public DateTime? LicenseKeyExpiryDate { get; private set; }

    private readonly List<Branch> _branches = [];
    public IReadOnlyCollection<Branch> Branches => _branches.AsReadOnly();

    private readonly List<User> _users = [];
    public IReadOnlyCollection<User> Users => _users.AsReadOnly();

    private readonly List<FlowRule> _flowRules = [];
    public IReadOnlyCollection<FlowRule> FlowRules => _flowRules.AsReadOnly();

    private Business() { } // EF Constructor

    public static Business Create(
        string name,
        string description,
        string businessRegistrationNumber,
        TIN taxIdentificationNumber,
        Address registeredAddress,
        string invoicePrefix,
        string contactEmail,
        Guid? adminUserId,
        Guid createdBy,
        string contactPhone,
        string serviceId,
        string industry,
        Guid firsBusinessId)
    {
        ValidateInputs(name, description, invoicePrefix, businessRegistrationNumber, contactEmail);

        return new Business
        {
            Name = name,
            Description = description,
            BusinessRegistrationNumber = businessRegistrationNumber,
            TaxIdentificationNumber = taxIdentificationNumber,
            RegisteredAddress = registeredAddress,
            InvoicePrefix = invoicePrefix,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            AdminUserId = adminUserId,
            ServiceId = serviceId,
            Status = BusinessStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            FIRSBusinessId = firsBusinessId,
            Industry = industry
        };
    }

    public void AddFirsConfiguration(string firsApiKey, string firsClientSecret)
    {
        FIRSApiKey = firsApiKey;
        FIRSClientSecret = firsClientSecret;
    }

    public void AddBranch(Branch branch)
    {
        _branches.Add(branch);
    }

    public void Activate(Guid? updatedBy)
    {
        Status = BusinessStatus.Active;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.Now;
    }

    public void Deactivate(Guid? updatedBy)
    {
        Status = BusinessStatus.Inactive;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.Now;
    }

    // User Management Methods
    public void Update(
        string description,
        string invoicePrefix,
        string contactEmail,
        Address address,
        Guid updatedBy,
        string? contactPhone = null)
    {
        ValidateInputs(string.Empty, description, invoicePrefix, BusinessRegistrationNumber, contactEmail, true);

        Description = description;
        InvoicePrefix = invoicePrefix;
        ContactEmail = contactEmail;
        RegisteredAddress = address;
        ContactPhone = contactPhone ?? "";
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.Now;
    }

    public void ChangeAdmin(Guid newAdminUserId, Guid changedBy)
    {
        if (newAdminUserId == AdminUserId)
            throw new InvalidOperationException("New admin is the same as current admin");

        AdminUserId = newAdminUserId;
        UpdatedBy = changedBy;
        UpdatedAt = DateTimeOffset.Now;
    }

    public void SetAdmin(Guid adminUserId, Guid assignedBy)
    {
        if (AdminUserId.HasValue && AdminUserId.Value == adminUserId)
            throw new ConflictException("User is already the admin of this business");

        AdminUserId = adminUserId;
        UpdatedBy = assignedBy;
        UpdatedAt = DateTimeOffset.Now;
    }

    // Access control methods
    public bool IsAdminUser(Guid userId) => AdminUserId == userId;
    public bool IsOwner(Guid userId) => IsAdminUser(userId);
    public bool IsUserInBusiness(Guid userId) => _users.Any(u => u.Id == userId);

    public bool IsUserInBusinessOrBranches(Guid userId)
    {
        // Check if user is in parent business
        if (IsUserInBusiness(userId))
            return true;

        // Check all branches
        return _branches.Any(branch => branch.IsUserInBranch(userId));
    }

    public bool CanUserAccessBranch(Guid userId, Guid branchId)
    {
        // Business admin can access all branches
        if (IsAdminUser(userId))
            return _branches.Any(b => b.Id == branchId);

        // Check if user belongs to the specific branch
        var branch = _branches.FirstOrDefault(b => b.Id == branchId);
        return branch?.IsUserInBranch(userId) ?? false;
    }

    public IEnumerable<Guid> GetAccessibleBranchIds(Guid userId)
    {
        // Business admin can access all branches
        if (IsAdminUser(userId))
            return _branches.Select(b => b.Id);

        // Regular users can only access their own branch
        var userBranch = _branches.FirstOrDefault(b => b.IsUserInBranch(userId));
        return userBranch != null ? [userBranch.Id] : [];
    }

    public bool CanManageUsers(Guid currentUserId, Guid? targetBranchId = null)
    {
        // Business admin can manage all users (parent + all branches)
        if (IsAdminUser(currentUserId))
            return true;

        // If targeting a specific branch, check if user is branch admin
        if (targetBranchId.HasValue)
        {
            var branch = _branches.FirstOrDefault(b => b.Id == targetBranchId.Value);
            return branch?.IsAdminUser(currentUserId) ?? false;
        }

        return false;
    }

    public void AssignFlowRule(Guid flowRuleId, Guid assignedBy)
    {
        if (flowRuleId == Guid.Empty)
            throw new InvalidOperationException("Invalid FlowRule ID");

        // Allow multiple FlowRules per business by updating the current FlowRuleId
        // This keeps track of the most recently assigned FlowRule for backward compatibility
        FlowRuleId = flowRuleId;
        UpdatedBy = assignedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets all active FlowRules for this business
    /// </summary>
    public IEnumerable<FlowRule> GetActiveFlowRules()
    {
        return _flowRules.Where(fr => !fr.IsDeleted);
    }

    /// <summary>
    /// Gets FlowRules that match a specific invoice amount
    /// </summary>
    public IEnumerable<FlowRule> GetApplicableFlowRules(double invoiceAmount)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return GetActiveFlowRules().Where(fr => invoiceAmount >= fr.Amount);
#pragma warning restore CS0618 // Type or member is obsolete
    }

    /// <summary>
    /// Gets the most recently created FlowRule (for backward compatibility)
    /// </summary>
    public FlowRule? GetCurrentFlowRule()
    {
        return GetActiveFlowRules().OrderByDescending(fr => fr.CreatedAt).FirstOrDefault();
    }

    /// <summary>
    /// Gets the count of active FlowRules for this business
    /// </summary>
    public int GetFlowRuleCount()
    {
        return GetActiveFlowRules().Count();
    }

    public bool HasActiveSubscription()
    {
        return SubscriptionId.HasValue && (Subscription?.IsActive() ?? false);
    }    

    public void AssignSubscription(Guid subscriptionId, Guid assignedBy)
    {
        if (SubscriptionId.HasValue)
            throw new InvalidOperationException("Business already has a subscription assigned");

        SubscriptionId = subscriptionId;
        UpdatedBy = assignedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateSubscription(Guid newSubscriptionId, Guid updatedBy)
    {
        if (!SubscriptionId.HasValue)
            throw new InvalidOperationException("Business does not have a subscription to update. Use AssignSubscription instead.");

        SubscriptionId = newSubscriptionId;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ValidateSubscriptionAccess()
    {
        if (!HasActiveSubscription())
            throw new InvalidOperationException("Business subscription is not active. Please contact support or renew your subscription.");
    }

    // API Key Management Methods
    public void SetApiKey(string? apiKey, Guid generatedBy)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new BadRequestException("API key cannot be null or empty", nameof(apiKey));

        if (IsApiKeyActive && !string.IsNullOrEmpty(ApiKey))
            throw new InvalidOperationException("Business already has an active API key");

        ApiKey = apiKey;
        ApiKeyGeneratedAt = DateTimeOffset.UtcNow;
        IsApiKeyActive = true;
        UpdatedBy = generatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RevokeApiKey(Guid revokedBy)
    {
        if (!IsApiKeyActive || string.IsNullOrEmpty(ApiKey))
        {
            throw new InvalidOperationException("No active API key to revoke");
        }

        ApiKey = null;
        IsApiKeyActive = false;
        UpdatedBy = revokedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignFirsApiConfiguration(Guid firsApiConfigurationId, Guid assignedBy)
    {
        BusinessFIRSApiConfigurationId = firsApiConfigurationId;
        UpdatedBy = assignedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RecordApiKeyUsage()
    {
        ApiKeyLastUsedAt = DateTimeOffset.UtcNow;
    }

    public bool HasValidApiKey()
    {
        return !string.IsNullOrWhiteSpace(ApiKey) && IsApiKeyActive;
    }

    public bool HasFIRSServiceId()
    {
        return !string.IsNullOrWhiteSpace(ServiceId) && ServiceId.Length == 8;
    }

    public void SetQrCodeKeys(string publicKey, string certificate)
    {
        PublicKey = publicKey;
        Certificate = certificate;
    }

    // License Management Methods (On-Premise Deployments)
    public void SetDeploymentMode(DeploymentMode deploymentMode, Guid updatedBy)
    {
        DeploymentMode = deploymentMode;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignLicense(string licenseKey, DateTime issuedDate, DateTime expiryDate, Guid assignedBy)
    {
        if (DeploymentMode != DeploymentMode.OnPremise)
            throw new InvalidOperationException("License can only be assigned to OnPremise deployment mode");
        if (string.IsNullOrWhiteSpace(licenseKey))
            throw new BadRequestException("License key cannot be null or empty", nameof(licenseKey));

        if (expiryDate <= issuedDate)
            throw new BadRequestException("License expiry date must be after issued date", nameof(expiryDate));

        if (expiryDate <= DateTime.UtcNow)
            throw new BadRequestException("License expiry date must be in the future", nameof(expiryDate));

        LicenseKey = licenseKey;
        LicenseKeyIssuedDate = issuedDate;
        LicenseKeyExpiryDate = expiryDate;
        UpdatedBy = assignedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RevokeLicense(Guid revokedBy)
    {
        if (string.IsNullOrWhiteSpace(LicenseKey))
            throw new InvalidOperationException("No license to revoke");

        LicenseKey = null;
        LicenseKeyIssuedDate = null;
        LicenseKeyExpiryDate = null;
        UpdatedBy = revokedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool HasValidLicense()
    {
        if (DeploymentMode != DeploymentMode.OnPremise)
            return true; // SaaS doesn't require license

        return !string.IsNullOrWhiteSpace(LicenseKey) &&
               LicenseKeyExpiryDate.HasValue &&
               LicenseKeyExpiryDate.Value > DateTime.UtcNow;
    }

    public bool IsLicenseExpired()
    {
        if (DeploymentMode != DeploymentMode.OnPremise)
            return false; // SaaS doesn't have license

        return LicenseKeyExpiryDate.HasValue &&
               LicenseKeyExpiryDate.Value <= DateTime.UtcNow;
    }

    private static void ValidateInputs(string name, string description, string invoicePrefix, string businessRegistrationNumber, string contactEmail, bool isUpdate = false)
    {
        if (!isUpdate)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new BadRequestException("Business name is required", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description))
            throw new BadRequestException("Business description is required", nameof(description));

        if (string.IsNullOrWhiteSpace(invoicePrefix))
            throw new BadRequestException("Invoice Prefix is required", nameof(invoicePrefix));

        if (string.IsNullOrWhiteSpace(businessRegistrationNumber))
            throw new BadRequestException("Business registration number is required", nameof(businessRegistrationNumber));

        if (string.IsNullOrWhiteSpace(contactEmail))
            throw new BadRequestException("Contact email is required", nameof(contactEmail));

        if (!contactEmail.Contains('@'))
            throw new BadRequestException("Invalid email format", nameof(contactEmail));

        if (name.Length > 200)
            throw new BadRequestException("Business name cannot exceed 200 characters", nameof(name));

        if (description.Length > 500)
            throw new BadRequestException("Business description cannot exceed 500 characters", nameof(description));
    }
}