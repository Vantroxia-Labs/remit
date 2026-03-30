using AegisEInvoicing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AegisEInvoicing.ERP.API.Extensions;

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

            // Set command timeout to 5 minutes for migrations (some ALTER COLUMN operations can be slow)
            context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            await context.Database.MigrateAsync();

            logger.LogInformation("Database migrated successfully");

            // Seed initial data if needed
            await SeedDataAsync(context, logger);
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating the database");
            throw;
        }
    }

    private static async Task SeedDataAsync(ApplicationDbContext context, ILogger logger)
    {
        logger.LogInformation("Seeding initial data...");

        // Add seed data here

        await context.SaveChangesAsync();
        logger.LogInformation("Initial data seeded successfully");
    }
}