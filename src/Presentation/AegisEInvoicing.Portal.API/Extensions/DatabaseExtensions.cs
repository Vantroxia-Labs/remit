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
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    private static async Task SeedData(ApplicationDbContext context, ILogger logger, IConfiguration configuration)
    {
        try
        {
            logger.LogInformation("Checking for existing seed data...");

            // Check if we already have users seeded (to avoid duplicates)
            var existingUsersCount = await context.Users.CountAsync();

            if (existingUsersCount > 0)
            {
                logger.LogInformation("Seed data already exists. Skipping seeding.");
                return;
            }

            logger.LogInformation("Starting comprehensive seed data process...");

            // Step 1: Seed Users first (needed for CreatedBy references in roles)
            logger.LogInformation("Seeding initial user data...");
            var adminEmail = configuration["AegisAdmin:Email"];
            var seedUsers = UserSeedData.GetSeedUsers(adminEmail);
            logger.LogInformation("Adding {UserCount} users to database", seedUsers.Count);

            var user = seedUsers.FirstOrDefault();

            if (user is null)
                throw new ArgumentNullException(nameof(user));

            await context.Users.AddAsync(user);

            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {UserCount} users", seedUsers.Count);

            // Step 2: Seed Platform Roles (now that system user exists)
            logger.LogInformation("Seeding platform roles...");
          
            var seedRoles = UserSeedData.GetSeedPlatformRoles(user.Id);
            logger.LogInformation("Adding {RoleCount} platform roles to database", seedRoles.Count);

            foreach (var role in seedRoles)
            {
                await context.PlatformRoles.AddAsync(role);
                logger.LogDebug("Added platform role: {RoleName} ({Category})",
                    role.Name, role.Category);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded {RoleCount} platform roles", seedRoles.Count);

            // Step 3: Seed User Role Assignments (depends on both users and roles)
            logger.LogInformation("Seeding user role assignments...");

            // Get the actual user and role from the database
            var systemAdminRole = await context.PlatformRoles
                .FirstOrDefaultAsync(r => r.Name == RoleConstants.AegisAdmin);
            
            if (systemAdminRole != null && user != null)
            {
                // Create role assignment using actual database IDs
                var superAdminAssignment = UserRoleAssignment.Create(
                    userId: user.Id,
                    platformRoleId: systemAdminRole.Id,
                    assignedBy: user.Id, // Use the actual user ID as assignedBy
                    expiresAt: null // No expiration for system admin
                );
                
                await context.UserRoleAssignments.AddAsync(superAdminAssignment);
                logger.LogDebug("Added role assignment: User {UserId} -> Role {RoleId}",
                    superAdminAssignment.UserId, superAdminAssignment.PlatformRoleId);
                
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully seeded role assignments");
            }
            else
            {
                logger.LogWarning("Could not create role assignment - AegisAdmin role or user not found");
            }

            // Step 4: Seed Platform Subscriptions
            logger.LogInformation("Seeding user role assignments...");

            var platformSubscriptions = UserSeedData.GetPlatformSubscriptions(user!.Id);
            foreach (var platformSubscription in platformSubscriptions)
            {
                await context.PlatformSubscriptions.AddAsync(platformSubscription);
                logger.LogDebug("Added Platform Subscription: Plan Name {PlanName} -> Tier {Tier}",
                platformSubscription.PlanName, platformSubscription.Tier);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Successfully seeded role assignments");

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