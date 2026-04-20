namespace AegisEInvoicing.Domain.Constants;

/// <summary>
/// Constants for system permissions used throughout the application
/// These permissions are tenant-scoped and enforce proper authorization
/// </summary>
public static class PermissionConstants
{
    // User Management Permissions
    public const string CreateUsers = "users.create";
    public const string ViewUsers = "users.view";
    public const string UpdateUsers = "users.update";
    public const string DeleteUsers = "users.delete";
    public const string ActivateUsers = "users.activate";
    public const string DeactivateUsers = "users.deactivate";
    public const string ResetPasswords = "users.reset_password";

    // Role Management Permissions
    public const string CreateRoles = "roles.create";
    public const string ViewRoles = "roles.view";
    public const string UpdateRoles = "roles.update";
    public const string DeleteRoles = "roles.delete";
    public const string AssignRoles = "roles.assign";
    public const string RevokeRoles = "roles.revoke";

    // Aegis User Management Permissions (Platform-level only)
    public const string CreateAegisUsers = "Aegis_users.create";
    public const string ViewAegisUsers = "Aegis_users.view";
    public const string UpdateAegisUserProfiles = "Aegis_users.update_profile";
    public const string UpdateAegisUserRoles = "Aegis_users.update_role";
    public const string DeleteAegisUsers = "Aegis_users.delete";
    public const string ActivateAegisUsers = "Aegis_users.activate";
    public const string DeactivateAegisUsers = "Aegis_users.deactivate";
    public const string ResetAegisUserPasswords = "Aegis_users.reset_password";

    // Invoice Management Permissions
    public const string CreateInvoices = "invoices.create";
    public const string ViewInvoices = "invoices.view";
    public const string UpdateInvoices = "invoices.update";
    public const string DeleteInvoices = "invoices.delete";
    public const string SubmitInvoices = "invoices.submit";
    public const string ApproveInvoices = "invoices.approve";
    public const string RejectInvoices = "invoices.reject";

    // Party (Customer/Supplier) Management Permissions
    public const string CreateParties = "parties.create";
    public const string ViewParties = "parties.view";
    public const string UpdateParties = "parties.update";
    public const string DeleteParties = "parties.delete";

    // Item / Product Management Permissions
    public const string CreateItems = "items.create";
    public const string ViewItems = "items.view";
    public const string UpdateItems = "items.update";
    public const string DeleteItems = "items.delete";

    // Business Management Permissions
    public const string ViewBusiness = "business.view";
    public const string UpdateBusiness = "business.update";
    public const string ManageBusinessSettings = "business.manage_settings";
    public const string ManageBranches = "business.manage_branches";
    public const string ManageCertificates = "business.manage_certificates";

    // Tenant Management Permissions
    public const string ManageTenant = "tenant.manage";
    public const string ViewTenantSettings = "tenant.view_settings";
    public const string UpdateTenantSettings = "tenant.update_settings";

    // System Permissions
    public const string ViewAuditLogs = "system.view_audit_logs";
    public const string ViewIntegrationLogs = "system.view_integration_logs";
    public const string ManageIntegrations = "system.manage_integrations";

    /// <summary>
    /// Permissions a ClientAdmin is allowed to include when building custom business roles.
    /// Deliberately excludes: certificates (business.manage_certificates), tenant management,
    /// system integrations, and Aegis platform user management.
    /// business.manage_settings IS included so ClientAdmin can delegate app/env switching.
    ///</summary>
    public static readonly string[] ClientAdminAssignablePermissions = [
        // Invoice operations
        CreateInvoices, ViewInvoices, UpdateInvoices, DeleteInvoices,
        SubmitInvoices, ApproveInvoices, RejectInvoices,

        // Party (customer/supplier) management
        CreateParties, ViewParties, UpdateParties, DeleteParties,

        // Item / product management
        CreateItems, ViewItems, UpdateItems, DeleteItems,

        // User management within the business
        CreateUsers, ViewUsers, UpdateUsers, DeleteUsers,
        ActivateUsers, DeactivateUsers, ResetPasswords,

        // Business info and settings (no certs/env — those stay platform-only)
        ViewBusiness, UpdateBusiness, ManageBranches, ManageBusinessSettings,

        // Reporting / audit (read-only system access)
        ViewAuditLogs, ViewIntegrationLogs,
    ];

    /// <summary>
    /// Gets all available system permissions
    /// </summary>
    public static readonly string[] AllPermissions = [
        // User Management
        CreateUsers, ViewUsers, UpdateUsers, DeleteUsers,
        ActivateUsers, DeactivateUsers, ResetPasswords,
        
        // Role Management
        CreateRoles, ViewRoles, UpdateRoles, DeleteRoles,
        AssignRoles, RevokeRoles,
        
        // Aegis User Management
        CreateAegisUsers, ViewAegisUsers, UpdateAegisUserProfiles, UpdateAegisUserRoles,
        DeleteAegisUsers, ActivateAegisUsers, DeactivateAegisUsers, ResetAegisUserPasswords,
        
        // Invoice Management
        CreateInvoices, ViewInvoices, UpdateInvoices, DeleteInvoices,
        SubmitInvoices, ApproveInvoices, RejectInvoices,

        // Party Management
        CreateParties, ViewParties, UpdateParties, DeleteParties,

        // Item Management
        CreateItems, ViewItems, UpdateItems, DeleteItems,
        
        // Business Management
        ViewBusiness, UpdateBusiness, ManageBusinessSettings,
        ManageBranches, ManageCertificates,
        
        // Tenant Management
        ManageTenant, ViewTenantSettings, UpdateTenantSettings,
        
        // System
        ViewAuditLogs, ViewIntegrationLogs, ManageIntegrations
    ];

    /// <summary>
    /// Gets permissions for Platform Admin role (Aegis users)
    /// </summary>
    public static readonly string[] PlatformAdminPermissions = [
        // User Management - Full control
        CreateUsers, ViewUsers, UpdateUsers, DeleteUsers,
        ActivateUsers, DeactivateUsers, ResetPasswords,
        
        // Role Management - Full control
        CreateRoles, ViewRoles, UpdateRoles, DeleteRoles,
        AssignRoles, RevokeRoles,
        
        // Aegis User Management - Full control (Platform-level only)
        CreateAegisUsers, ViewAegisUsers, UpdateAegisUserProfiles, UpdateAegisUserRoles,
        DeleteAegisUsers, ActivateAegisUsers, DeactivateAegisUsers, ResetAegisUserPasswords,
        
        // Invoice Management - Full control
        CreateInvoices, ViewInvoices, UpdateInvoices, DeleteInvoices,
        SubmitInvoices, ApproveInvoices, RejectInvoices,
        
        // Business Management - Full control
        ViewBusiness, UpdateBusiness, ManageBusinessSettings,
        ManageBranches, ManageCertificates,
        
        // Tenant Management - Full control
        ManageTenant, ViewTenantSettings, UpdateTenantSettings,
        
        // System - Read access
        ViewAuditLogs, ViewIntegrationLogs, ManageIntegrations
    ];

    /// <summary>
    /// Gets permissions for Business Admin role
    /// </summary>
    public static readonly string[] BusinessAdminPermissions = [
        // Business Management - Full control
        ViewBusiness, UpdateBusiness, ManageBusinessSettings,
        ManageBranches, ManageCertificates,
        
        // Invoice Management - Full control
        CreateInvoices, ViewInvoices, UpdateInvoices, DeleteInvoices,
        SubmitInvoices, ApproveInvoices, RejectInvoices,
        
        // Limited User Management
        ViewUsers,
        
        // System - Read access
        ViewIntegrationLogs
    ];

    /// <summary>
    /// Gets permissions for Invoice Manager role
    /// </summary>
    public static readonly string[] InvoiceManagerPermissions = [
        // Invoice Management - Full control
        CreateInvoices, ViewInvoices, UpdateInvoices, DeleteInvoices,
        SubmitInvoices, ApproveInvoices, RejectInvoices,
        
        // Business - Read access
        ViewBusiness,
        
        // System - Limited access
        ViewIntegrationLogs
    ];

    /// <summary>
    /// Gets permissions for User role
    /// </summary>
    public static readonly string[] UserPermissions = [
        // Invoice Management - Basic operations
        CreateInvoices, ViewInvoices, UpdateInvoices, SubmitInvoices,
        
        // Business - Read access
        ViewBusiness
    ];

    /// <summary>
    /// Gets permissions for Viewer role
    /// </summary>
    public static readonly string[] ViewerPermissions = [
        // Read-only access
        ViewInvoices, ViewBusiness
    ];
}