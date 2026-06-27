using AegisEInvoicing.Domain.Common;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AegisEInvoicing.Portal.API.Extensions;

/// <summary>
/// Database migration extensions
/// </summary>
public static class DatabaseExtensions
{
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();

            logger.LogInformation("Migrating database...");

            try
            {
                // Set command timeout to 5 minutes for migrations (some ALTER COLUMN operations can be slow)
                context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrated successfully");
            }
            catch (Exception sqlEx) when (sqlEx.Message.Contains("already exists") || sqlEx.Message.Contains("42P07"))
            {
                logger.LogWarning("Migration conflict detected: {Message}. This usually means the database schema already exists.", sqlEx.Message);
                logger.LogInformation("Attempting to resolve migration conflict...");

                // Try to get pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogWarning("Found {Count} pending migrations. You may need to manually mark them as applied.", pendingMigrations.Count());
                    foreach (var migration in pendingMigrations)
                    {
                        logger.LogWarning("Pending migration: {Migration}", migration);
                    }
                }

                logger.LogError("Migration failed due to existing schema. Please run the fix_migration.sql script or manually resolve the conflict.");
                throw new InvalidOperationException(
                    "Database migration failed because tables already exist. " +
                    "Run the fix_migration.sql script to resolve this issue.", sqlEx);
            }

            // Seed initial data if needed
            var configuration = services.GetRequiredService<IConfiguration>();
            await SeedData(context, logger, configuration);

            // Always reconcile system role permissions (fixes stale seed data on existing DBs)
            await ReconcileSystemRolePermissions(context, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    public static async Task SeedDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var logger = services.GetRequiredService<ILogger<Program>>();
            var configuration = services.GetRequiredService<IConfiguration>();

            logger.LogInformation("Seeding database...");
            await SeedData(context, logger, configuration);
            await ReconcileSystemRolePermissions(context, logger);
            logger.LogInformation("Database seeded successfully.");
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }

    private static async Task SeedData(ApplicationDbContext context, ILogger logger, IConfiguration configuration)
    {
        try
        {
            logger.LogInformation("Starting comprehensive seed data process...");

            var adminEmail = configuration["AegisAdmin:Email"];
            var seedUsers = UserSeedData.GetSeedUsers(adminEmail);
            var seedUser = seedUsers.FirstOrDefault();

            if (seedUser is null)
                throw new ArgumentNullException(nameof(seedUser));

            // Step 1: Seed Users
            logger.LogInformation("Checking user seed data...");
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == seedUser.Email);
            var actualUser = existingUser;

            if (existingUser == null)
            {
                logger.LogInformation("Seeding initial user data...");
                await context.Users.AddAsync(seedUser);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully seeded users");
                actualUser = seedUser;
            }
            else
            {
                logger.LogInformation("Seed user already exists. Skipping user seeding.");
            }

            // Step 2: Seed Platform Roles
            logger.LogInformation("Checking platform roles...");
            var existingRolesCount = await context.PlatformRoles.CountAsync();
            var seedRoles = UserSeedData.GetSeedPlatformRoles(actualUser!.Id);

            if (existingRolesCount == 0)
            {
                logger.LogInformation("Seeding platform roles...");
                foreach (var role in seedRoles)
                {
                    await context.PlatformRoles.AddAsync(role);
                    logger.LogDebug("Added platform role: {RoleName} ({Category})", role.Name, role.Category);
                }
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully seeded {RoleCount} platform roles", seedRoles.Count);
            }
            else
            {
                logger.LogInformation("Platform roles already exist. Skipping role seeding.");
            }

            // Step 3: Seed User Role Assignments
            logger.LogInformation("Checking user role assignments...");
            var systemAdminRole = await context.PlatformRoles.FirstOrDefaultAsync(r => r.Name == RoleConstants.AegisAdmin);

            if (systemAdminRole != null && actualUser != null)
            {
                var existingAssignment = await context.UserRoleAssignments
                    .FirstOrDefaultAsync(ra => ra.UserId == actualUser.Id && ra.PlatformRoleId == systemAdminRole.Id);

                if (existingAssignment == null)
                {
                    logger.LogInformation("Seeding user role assignments...");
                    var superAdminAssignment = UserRoleAssignment.Create(
                        userId: actualUser.Id,
                        platformRoleId: systemAdminRole.Id,
                        assignedBy: actualUser.Id,
                        expiresAt: null
                    );

                    await context.UserRoleAssignments.AddAsync(superAdminAssignment);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Successfully seeded role assignments");
                }
                else
                {
                    logger.LogInformation("User role assignment already exists. Skipping assignment seeding.");
                }
            }
            else
            {
                logger.LogWarning("Could not create role assignment - AegisAdmin role or user not found");
            }

            // Step 4: Seed Platform Subscriptions
            logger.LogInformation("Checking platform subscriptions...");
            var existingSubscriptionsCount = await context.PlatformSubscriptions.CountAsync();

            if (existingSubscriptionsCount == 0)
            {
                logger.LogInformation("Seeding platform subscriptions...");
                var platformSubscriptions = UserSeedData.GetPlatformSubscriptions(actualUser!.Id);
                foreach (var platformSubscription in platformSubscriptions)
                {
                    await context.PlatformSubscriptions.AddAsync(platformSubscription);
                    logger.LogDebug("Added Platform Subscription: Plan Name {PlanName} -> Tier {Tier}",
                        platformSubscription.PlanName, platformSubscription.Tier);
                }
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully seeded platform subscriptions");
            }
            else
            {
                logger.LogInformation("Platform subscriptions already exist. Skipping subscription seeding.");
            }

            logger.LogInformation("Comprehensive seeding completed successfully");

            // Log summary of all seeded data
            await LogSeedSummary(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding initial data");
            throw;
        }
    }

    private static async Task ReconcileSystemRolePermissions(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            // Ensure the platform-wide ClientAdmin role has all expected permissions.
            // This runs every startup so stale seed data on existing databases is corrected.
            var clientAdminRole = await context.PlatformRoles
                .FirstOrDefaultAsync(r => r.Name == RoleConstants.ClientAdmin && r.BusinessId == null);

            if (clientAdminRole == null) return;

            var expectedPermissions = PermissionConstants.ClientAdminAssignablePermissions;
            bool changed = false;

            foreach (var perm in expectedPermissions)
            {
                if (!clientAdminRole.HasPermission(perm))
                {
                    clientAdminRole.AddPermission(perm);
                    changed = true;
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Reconciled ClientAdmin role: added missing permissions");
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not reconcile system role permissions");
        }
    }

    private static async Task LogSeedSummary(ApplicationDbContext context, ILogger logger)
    {
        try
        {
            var totalUsers = await context.Users.CountAsync();
            var AegisUsers = await context.Users.CountAsync(u => u.IsAegisUser);
            var activeUsers = await context.Users.CountAsync(u => u.Status == UserStatus.Active);
            var pendingUsers = await context.Users.CountAsync(u => u.Status == UserStatus.PendingActivation);
            var totalRoles = await context.PlatformRoles.CountAsync();
            var totalAssignments = await context.UserRoleAssignments.CountAsync();

            logger.LogInformation("=== SEED SUMMARY ===");
            logger.LogInformation("Users - Total: {Total}, Aegis: {Aegis}, Active: {Active}, Pending: {Pending}",
                totalUsers, AegisUsers, activeUsers, pendingUsers);
            logger.LogInformation("Platform Roles: {TotalRoles}", totalRoles);
            logger.LogInformation("Role Assignments: {TotalAssignments}", totalAssignments);
            logger.LogInformation("===================");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not generate seed summary");
        }
    }
}