using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Events.UserManagement;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Domain.Entities.UserManagement;

/// <summary>
/// Represents a user within a business and optionally a branch
/// Users can be at business level (managed by business admin) or branch level (managed by branch admin)
/// </summary>
public class User : AuditableAggregateRoot
{
    private readonly List<UserRoleAssignment> _roleAssignments = [];

    public Guid? BusinessId { get; private set; } // Nullable for KMPG users who don't belong to any business
    public Guid? BranchId { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public UserStatus Status { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public int FailedLoginAttempts { get; private set; }
    public DateTimeOffset? LockedOutUntil { get; private set; }
    public DateTimeOffset? PasswordChangedAt { get; private set; }
    public bool MustChangePassword { get; private set; }
    public UserPreferences Preferences { get; private set; }

    // KMPG-specific properties (null for business users)
    public bool IsAegisUser { get; private set; }
    public AegisRole? AegisRole { get; private set; }
    public string? AegisEmployeeId { get; private set; }
    public string? AegisDepartment { get; private set; }
    public DateTimeOffset? LastAegisActivityAt { get; private set; }

    // Navigation properties
    public Business? Business { get; private set; } // Nullable for KMPG users
    public Branch? Branch { get; private set; }
    public IReadOnlyCollection<UserRoleAssignment> RoleAssignments => _roleAssignments.AsReadOnly();

    public override string ToString() => $"{FirstName} {LastName}";

    // Parameterless constructor for Entity Framework
    private User()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
        Email = string.Empty;
        PasswordHash = null!; // Will be set by EF when loading
        Status = UserStatus.Active;
        IsEmailVerified = false;
        FailedLoginAttempts = 0;
        MustChangePassword = true;
        Preferences = UserPreferences.Default();
        PasswordChangedAt = DateTimeOffset.UtcNow;
    }

    private User(
        Guid? businessId, // Nullable for KMPG users
        string firstName,
        string lastName,
        string email,
        PasswordHash passwordHash,
        Guid? branchId = null)
    {
        BusinessId = businessId;
        BranchId = branchId;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PasswordHash = passwordHash;
        Status = UserStatus.PendingActivation;
        IsEmailVerified = false;
        FailedLoginAttempts = 0;
        MustChangePassword = true;
        Preferences = UserPreferences.Default();
        PasswordChangedAt = DateTimeOffset.UtcNow;
    }

    public static User Create(
        Guid? businessId,
        string firstName,
        string lastName,
        string email,
        PasswordHash passwordHash,
        Guid createdBy,
        Guid? branchId = null,
        string? phoneNumber = null)
    {
        ValidateInputs(firstName, lastName, email);

        var user = new User(businessId, firstName, lastName, email, passwordHash, branchId)
        {
            PhoneNumber = phoneNumber,
            Status = UserStatus.Active
        };

        user.AddDomainEvent(new UserCreatedEvent(
            user.Id, 
            user.BusinessId, 
            user.Email, 
            user.FirstName, 
            user.LastName,
            createdBy,
            DateTimeOffset.UtcNow));

        return user;
    }

    /// <summary>
    /// Creates a new KMPG user who is not tied to any business
    /// </summary>
    public static User CreateAegisUser(
        string firstName,
        string lastName,
        string email,
        PasswordHash passwordHash,
        AegisRole AegisRole,
        Guid createdBy,
        string? phoneNumber = null,
        string? AegisEmployeeId = null,
        string? AegisDepartment = null)
    {
        ValidateInputs(firstName, lastName, email);

        var user = new User(null, firstName, lastName, email, passwordHash, null) // No business or branch
        {
            PhoneNumber = phoneNumber,
            IsAegisUser = true,
            AegisRole = AegisRole,
            AegisEmployeeId = AegisEmployeeId,
            AegisDepartment = AegisDepartment,
            LastAegisActivityAt = DateTimeOffset.UtcNow
        };

        // KMPG users automatically get PlatformAdmin role in the standard role system
        user.AssignPlatformAdminRole(AegisRole);

        user.AddDomainEvent(new UserCreatedEvent(
            user.Id, 
            null, // No business ID for KMPG users
            user.Email, 
            user.FirstName, 
            user.LastName,
            createdBy,
            DateTimeOffset.UtcNow));

        return user;
    }

    public void Activate(Guid activatedBy)
    {
        if (Status == UserStatus.Active)
            throw new InvalidOperationException("User is already active");

        Status = UserStatus.Active;
        AddDomainEvent(new UserActivatedEvent(Id, BusinessId, Email, activatedBy, DateTimeOffset.UtcNow));
    }

    public void Deactivate(Guid deactivatedBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Deactivation reason is required", nameof(reason));

        if (Status == UserStatus.Inactive)
            throw new InvalidOperationException("User is already inactive");

        Status = UserStatus.Inactive;
        AddDomainEvent(new UserDeactivatedEvent(Id, BusinessId, Email, deactivatedBy, reason, DateTimeOffset.UtcNow));
    }

    public void ChangePassword(PasswordHash newPasswordHash, Guid changedBy, bool isReset = false)
    {
        ArgumentNullException.ThrowIfNull(newPasswordHash);

        PasswordHash = newPasswordHash;
        PasswordChangedAt = DateTimeOffset.UtcNow;
        MustChangePassword = false;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;

        AddDomainEvent(new UserPasswordChangedEvent(Id, BusinessId, Email, changedBy, isReset, DateTimeOffset.UtcNow));
    }

    public void ChangePassword(PasswordHash newPasswordHash, bool isReset = false)
    {
        ArgumentNullException.ThrowIfNull(newPasswordHash);

        PasswordHash = newPasswordHash;
        PasswordChangedAt = DateTimeOffset.UtcNow;
        MustChangePassword = false;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;

        AddDomainEvent(new UserPasswordChangedEvent(Id, BusinessId, Email, null, isReset, DateTimeOffset.UtcNow));
    }

    public void UpdateProfile(string firstName, string lastName, Guid updatedBy, string? phoneNumber = null)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        var changes = new Dictionary<string, object>();
        if (FirstName != firstName) changes["FirstName"] = firstName;
        if (LastName != lastName) changes["LastName"] = lastName;
        if (PhoneNumber != phoneNumber) changes["PhoneNumber"] = phoneNumber ?? "";

        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;

        if (changes.Count > 0)
        {
            AddDomainEvent(new UserProfileUpdatedEvent(Id, BusinessId, Email, updatedBy, changes, DateTimeOffset.UtcNow));
        }
    }

    public void Delete(Guid deletedBy, string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Deletion reason is required", nameof(reason));

        AddDomainEvent(new UserDeletedEvent(Id, BusinessId, Email, deletedBy, reason, DateTimeOffset.UtcNow));
    }

    public bool IsInBusiness(Guid businessId) => BusinessId == businessId;

    public bool IsInBusinessBranch(Guid branchId) => BranchId == branchId;

    public bool IsBusinessLevelUser() => !BranchId.HasValue;

    public bool IsBranchLevelUser() => BranchId.HasValue;

    public void TransferToBranch(Guid? newBranchId, Guid transferredBy)
    {
        if (newBranchId == BranchId)
            throw new InvalidOperationException("User is already assigned to this branch");

        var oldBranchId = BranchId;
        BranchId = newBranchId;

        AddDomainEvent(new UserBranchAssignedEvent(
            Id, 
            BusinessId, 
            Email, 
            oldBranchId, 
            newBranchId, 
            transferredBy, 
            DateTimeOffset.UtcNow));
    }

    public void RemoveFromBranch(Guid removedBy)
    {
        if (!BranchId.HasValue)
            throw new InvalidOperationException("User is not assigned to any branch");

        var oldBranchId = BranchId.Value;
        BranchId = null;

        AddDomainEvent(new UserBranchRemovedEvent(
            Id, 
            BusinessId, 
            Email, 
            oldBranchId, 
            removedBy, 
            DateTimeOffset.UtcNow));
    }

    public bool IsLocked() => LockedOutUntil.HasValue && LockedOutUntil.Value > DateTimeOffset.UtcNow;

    public bool CanLogin() => Status == UserStatus.Active && !IsLocked();

    public void RecordFailedLogin(string ipAddress)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 5)
        {
            LockedOutUntil = DateTimeOffset.UtcNow.AddMinutes(30);
        }
    }

    public void RecordSuccessfulLogin(string ipAddress)
    {
        LastLoginAt = DateTimeOffset.UtcNow;
        FailedLoginAttempts = 0;
        LockedOutUntil = null;
    }

    public void AssignRole(Guid platformRoleId, Guid assignedBy, DateTimeOffset? expiresAt = null)
    {
        if (_roleAssignments.Any(ra => ra.PlatformRoleId == platformRoleId && ra.IsValid()))
            throw new InvalidOperationException("User already has this role assigned");

        var assignment = UserRoleAssignment.Create(Id, platformRoleId, assignedBy, expiresAt);
        _roleAssignments.Add(assignment);

        AddDomainEvent(new UserRoleAssignedEvent(Id, BusinessId, Email, platformRoleId, assignedBy, DateTimeOffset.UtcNow));
    }

    public void RevokeRole(Guid platformRoleId, Guid revokedBy, string? reason = null)
    {
        var assignment = _roleAssignments.FirstOrDefault(ra => ra.PlatformRoleId == platformRoleId && ra.IsValid()) ?? throw new InvalidOperationException("User does not have this role assigned or it's already revoked");
        assignment.Revoke(revokedBy, reason);
        AddDomainEvent(new UserRoleRemovedEvent(Id, BusinessId, Email, platformRoleId, revokedBy, reason, DateTimeOffset.UtcNow));
    }

    public bool HasRole(Guid platformRoleId) => _roleAssignments.Any(ra => ra.PlatformRoleId == platformRoleId && ra.IsValid());

    public IEnumerable<Guid> GetActiveRoles() => _roleAssignments.Where(ra => ra.IsValid()).Select(ra => ra.PlatformRoleId);

    public void UpdateAegisActivity()
    {
        if (!IsAegisUser)
            throw new InvalidOperationException("Only KMPG users can have activity updated");

        LastAegisActivityAt = DateTimeOffset.UtcNow;
    }

    public void UpdateAegisRole(AegisRole newAegisRole, Guid updatedBy)
    {
        if (!IsAegisUser)
            throw new InvalidOperationException("Only KMPG users can have KMPG roles updated");

        var oldRole = AegisRole;
        AegisRole = newAegisRole;

        AddDomainEvent(new UserAegisRoleChangedEvent(Id, Email, oldRole, newAegisRole, updatedBy, DateTimeOffset.UtcNow));
    }

    public void UpdateAegisProfile(string? aegisEmployeeId, string? aegisDepartment, Guid updatedBy)
    {
        if (!IsAegisUser)
            throw new InvalidOperationException("Only KMPG users can have KMPG profile updated");

        var changes = new Dictionary<string, object>();
        if (AegisEmployeeId != aegisEmployeeId) changes["AegisEmployeeId"] = aegisEmployeeId ?? "";
        if (AegisDepartment != aegisDepartment) changes["AegisDepartment"] = aegisDepartment ?? "";

        AegisEmployeeId = aegisEmployeeId;
        AegisDepartment = aegisDepartment;

        if (changes.Count > 0)
        {
            AddDomainEvent(new UserAegisProfileUpdatedEvent(Id, Email, updatedBy, changes, DateTimeOffset.UtcNow));
        }
    }

    private void AssignPlatformAdminRole(AegisRole AegisRole)
    {
        // Map Aegis roles to platform admin capabilities
        // All Aegis users get platform admin privileges but with different scopes based on their KMPG role
        
        // The actual platform role assignment will be handled by a domain event handler 
        // that has access to the PlatformRole repository to assign the appropriate role
        
        // Create domain event indicating Aegis role assignment for a new user
        // The event handler will map this Aegis role to the appropriate PlatformRole:
        // - AegisAdmin → Super Admin (full system access)
        // - ComplianceOfficer → Business Manager (manage businesses and operations)  
        // - HelpDesk → Report Viewer (read-only access with reporting capabilities)
        // - Support → Technical Support (customer support with limited write access)
        AddDomainEvent(new UserAegisRoleChangedEvent(
            Id,
            Email,
            null, // oldRole - new user has no previous Aegis role
            AegisRole,
            CreatedBy,
            DateTimeOffset.UtcNow
        ));
    }

    private static void ValidateInputs(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));

        if (!email.Contains('@'))
            throw new ArgumentException("Invalid email format", nameof(email));
    }
}

public enum UserStatus
{
    PendingActivation,
    Active,
    Inactive,
    Suspended,
    Deleted
}