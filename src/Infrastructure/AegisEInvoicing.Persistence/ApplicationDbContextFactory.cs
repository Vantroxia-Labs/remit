using AegisEInvoicing.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace AegisEInvoicing.Persistence;

/// <summary>
/// Design-time factory for creating ApplicationDbContext for migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{   
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var configuration = new ConfigurationBuilder()
        .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"), optional: false, reloadOnChange: true)
        .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"), optional: true)
        .AddEnvironmentVariables()
        .Build();

        // Use a default connection string for design-time
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        optionsBuilder.UseNpgsql(connectionString);

        // Create mock services for design-time
        var mockCurrentUserService = new MockCurrentUserService();
        var mockDateTime = new MockDateTime();

        return new ApplicationDbContext(optionsBuilder.Options, mockCurrentUserService, mockDateTime);
    }

    private class MockCurrentUserService : ICurrentUserService
    {
        public Guid? UserId => Guid.CreateVersion7();
        public string? UserName => "system";
        public string? Email => "system@design.local";
        public bool IsAuthenticated => true;
        public IEnumerable<string> Roles => new[] { "Designer" };
        public IEnumerable<string> Permissions => new[] { "All" };
        public Guid? BusinessId => null;
        public Guid? BranchId => null;
        public bool IsBusinessLevel => false;
        public bool IsBranchLevel => false;
        public bool IsPlatformAdmin => true;
        public bool IsAegisUser => false;
        public string? AegisRole => null;
        public string? AegisEmployeeId => null;
        public string? AegisDepartment => null;

        public bool HasRole(string role) => true;
        public bool HasPermission(string permission) => true;
    }

    private class MockDateTime : IDateTime
    {
        public DateTimeOffset Now => DateTimeOffset.UtcNow;
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
        public DateTimeOffset Today => DateTimeOffset.UtcNow.Date;
        public DateOnly DateOnly => DateOnly.FromDateTime(DateTime.UtcNow);
        public TimeOnly TimeOnly => TimeOnly.FromDateTime(DateTime.UtcNow);
    }
}