using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Interfaces.Repositories;
using AegisEInvoicing.Domain.Constants;
using AegisEInvoicing.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisEInvoicing.Persistence;

/// <summary>
/// Persistence dependency injection configuration
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        var conString = configuration.GetConnectionString("DefaultConnection");

        // Build connection string with additional resilience parameters
        var builder = new Npgsql.NpgsqlConnectionStringBuilder(conString)
        {
            // Connection pooling settings
            MaxPoolSize = 100,
            MinPoolSize = 5,
            ConnectionIdleLifetime = 300, // 5 minutes
            ConnectionPruningInterval = 10, // 10 seconds

            // Timeout settings
            Timeout = 30, // Connection timeout
            CommandTimeout = 60, // Command timeout

            // Keep alive to prevent connection drops
            KeepAlive = 30, // Send keepalive every 30 seconds
            TcpKeepAlive = true,
            TcpKeepAliveTime = 30,
            TcpKeepAliveInterval = 10,

            // Reliability settings
            Pooling = true,
            NoResetOnClose = false
        };

        // Database context with retry policies
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                builder.ConnectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);

                    // Enable retry on failure for transient errors
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: InvoiceConstants.MAX_RETRY_ATTEMPTS,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);

                    // Set command timeout
                    npgsqlOptions.CommandTimeout(InvoiceConstants.DB_COMMAND_TIMEOUT_SECONDS);
                });

            // Only enable sensitive data logging in Development environment for security
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            if (environment.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
            }

            options.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Domain Services

        return services;
    }
}
