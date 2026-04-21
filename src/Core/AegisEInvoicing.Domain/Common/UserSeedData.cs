using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.ValueObjects.UserManagement;

namespace AegisEInvoicing.Domain.Common;

public static class UserSeedData
{
    // Predefined IDs for consistency
    private static readonly Guid SystemUserId = Guid.Parse("c0b8df9d-14c3-4d69-bb92-adee94bde64e");

    public static List<PlatformRole> GetSeedPlatformRoles(Guid actualCreatedBy)
    {
        var roles = new List<PlatformRole>();
        var createdBy = actualCreatedBy;

        // Super Admin Role - Full platform access
        var superAdminRole = PlatformRole.CreateSystemRole(
            name: "AegisAdmin",
            description: "Full platform administration access with all permissions",
            category: "Administrative",
            sortOrder: 1,
            createdBy: createdBy,
            permissions: PermissionConstants.PlatformAdminPermissions
        );
        roles.Add(superAdminRole);

        // Merchant Admin Role - Business-focused permissions
        var businessManagerRole = PlatformRole.Create(
            name: "ClientAdmin",
            description: "Manages business operations and user accounts within organizations",
            category: "Operational",
            sortOrder: 2,
            createdBy: createdBy
        );
        businessManagerRole.AddPermission("users.read");
        businessManagerRole.AddPermission("users.update");
        businessManagerRole.AddPermission("businesses.read");
        businessManagerRole.AddPermission("businesses.update");
        businessManagerRole.AddPermission(PermissionConstants.ManageBusinessSettings);
        businessManagerRole.AddPermission("invoices.read");
        businessManagerRole.AddPermission("invoices.create");
        businessManagerRole.AddPermission("invoices.update");
        businessManagerRole.AddPermission("invoices.submit");
        businessManagerRole.AddPermission("invoices.approve");
        businessManagerRole.AddPermission("reports.read");
        businessManagerRole.AddPermission("users.view");
        businessManagerRole.AddPermission("users.create");
        roles.Add(businessManagerRole);

        // Merchant Initiator Role - Business-focused permissions
        var businessInitiatorRole = PlatformRole.Create(
            name: "ClientUser",
            description: "Inititates invoice request within the business",
            category: "Operational",
            sortOrder: 3,
            createdBy: createdBy
        );
        businessInitiatorRole.AddPermission("invoices.create");
        roles.Add(businessInitiatorRole);

        return roles;
    }

    public static List<User> GetSeedUsers(string? adminEmail = null)
    {
        var users = new List<User>();
        var createdBy = SystemUserId;

        // Create password hash (assuming a helper method exists)
        var defaultPasswordHash = CreatePasswordHash("AegisAdmin@2026!");

        #region Aegis Users      

        // Aegis System Administrator
        var AegisSysAdmin = User.CreateAegisUser(
            firstName: "Godswill",
            lastName: "David",
            email: adminEmail ?? "david.godswill@aegis.com",
            passwordHash: defaultPasswordHash,
            AegisRole: AegisRole.AegisAdmin,
            createdBy: createdBy,
            phoneNumber: "+1-555-0305",
            AegisEmployeeId: "Aegis005",
            AegisDepartment: "IT Services"
        );
        AegisSysAdmin.Activate(createdBy);
        users.Add(AegisSysAdmin);

        #endregion       

        return users;
    }

    // Note: This method is deprecated - role assignments are now created directly in DatabaseExtensions.cs
    // using actual database IDs to avoid foreign key constraint violations
    [Obsolete("Use DatabaseExtensions.SeedData method instead which uses actual database IDs")]
    public static List<UserRoleAssignment> GetSeedUserRoleAssignments()
    {
        var assignments = new List<UserRoleAssignment>();
        var assignedBy = SystemUserId;

        // Get the Aegis admin user ID (assuming it's the first user)
        var users = GetSeedUsers();
        var AegisAdminUserId = users.First().Id;

        // Get platform roles to get their IDs
        var roles = GetSeedPlatformRoles(AegisAdminUserId);
        var superAdminRole = roles.First(r => r.Name == RoleConstants.AegisAdmin);

        // Assign Super Admin role to Aegis System Administrator
        var superAdminAssignment = UserRoleAssignment.Create(
            userId: AegisAdminUserId,
            platformRoleId: superAdminRole.Id,
            assignedBy: assignedBy,
            expiresAt: null // No expiration for system admin
        );
        assignments.Add(superAdminAssignment);

        return assignments;
    }

    public static List<PlatformSubscription> GetPlatformSubscriptions(Guid actualCreatedBy)
    {
        var platformSubscriptions = new List<PlatformSubscription>();

        #region PlatformSubscriptions

        // Invoice Portal - Invoice creation on portal only
        var portalPlan = PlatformSubscription.Create(
            planName: "Invoice Portal",
            tier: SubscriptionTier.SaaS,
            monthlyPrice: 100_000,
            createdBy: actualCreatedBy,
            annualPrice: 1_000_000
        );
        platformSubscriptions.Add(portalPlan);

        // File Manager - SFTP file-based invoice submission
        var sftpPlan = PlatformSubscription.Create(
            planName: "File Manager",
            tier: SubscriptionTier.SFTP,
            monthlyPrice: 120_000,
            createdBy: actualCreatedBy,
            annualPrice: 1_200_000
        );
        platformSubscriptions.Add(sftpPlan);

        // API Connect - API integration for invoice submission
        var apiPlan = PlatformSubscription.Create(
            planName: "API Connect",
            tier: SubscriptionTier.ApiOnly,
            monthlyPrice: 150_000,
            createdBy: actualCreatedBy,
            annualPrice: 1_500_000
        );
        platformSubscriptions.Add(apiPlan);

        #endregion

        return platformSubscriptions;
    }

    // Helper method - replace with your actual password hashing implementation
    private static PasswordHash CreatePasswordHash(string password)
    {
        return PasswordHash.Create(password); // Assuming constructor exists
    }
}