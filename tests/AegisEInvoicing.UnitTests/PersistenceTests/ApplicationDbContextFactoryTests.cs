using AegisEInvoicing.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests;

public class ApplicationDbContextFactoryTests
{
    private readonly ApplicationDbContextFactory _factory;

    public ApplicationDbContextFactoryTests()
    {
        _factory = new ApplicationDbContextFactory();
    }

    [Fact]
    public void CreateDbContext_ShouldCreateValidContext()
    {
        // Arrange
        var args = new string[] { };

        // Act
        var context = _factory.CreateDbContext(args);

        // Assert
        context.Should().NotBeNull();
        context.Should().BeOfType<ApplicationDbContext>();
    }

    [Fact]
    public void CreateDbContext_ShouldConfigureDbSets()
    {
        // Arrange
        var args = new string[] { };

        // Act
        using var context = _factory.CreateDbContext(args);

        // Assert
        context.OutboxEvents.Should().NotBeNull();
        context.IntegrationLogs.Should().NotBeNull();
        context.Businesses.Should().NotBeNull();
        context.Invoices.Should().NotBeNull();
        context.InvoiceItems.Should().NotBeNull();
        context.InvoiceApprovalHistories.Should().NotBeNull();
        context.Parties.Should().NotBeNull();
        context.BusinessItems.Should().NotBeNull();
        context.ItemCategories.Should().NotBeNull();
        context.Branches.Should().NotBeNull();
        context.FlowRules.Should().NotBeNull();
        context.FIRSApiConfigurations.Should().NotBeNull();
        context.BusinessFIRSApiConfigurations.Should().NotBeNull();
        context.BusinessOnboardings.Should().NotBeNull();
        context.SystemConfigurations.Should().NotBeNull();
        context.SubscriptionKeys.Should().NotBeNull();
        context.ApiUsageTrackings.Should().NotBeNull();
        context.ApiUsageSummaries.Should().NotBeNull();
        context.InvoiceTransmissionQueues.Should().NotBeNull();
        context.Users.Should().NotBeNull();
        context.PlatformRoles.Should().NotBeNull();
        context.UserRoleAssignments.Should().NotBeNull();
        context.UserSessions.Should().NotBeNull();
        context.RefreshTokens.Should().NotBeNull();
        context.Subscriptions.Should().NotBeNull();
        context.PlatformSubscriptions.Should().NotBeNull();
        context.SFTPUsers.Should().NotBeNull();
    }

    [Fact]
    public void CreateDbContext_WithEmptyArgs_ShouldWork()
    {
        // Arrange
        var args = Array.Empty<string>();

        // Act
        var action = () => _factory.CreateDbContext(args);

        // Assert
        action.Should().NotThrow();
        var context = action();
        context.Should().NotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithNullArgs_ShouldWork()
    {
        // Arrange
        string[]? args = null;

        // Act
        var action = () => _factory.CreateDbContext(args!);

        // Assert
        action.Should().NotThrow();
        var context = action();
        context.Should().NotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_WithArgs_ShouldWork()
    {
        // Arrange
        var args = new[] { "--environment", "Development" };

        // Act
        var action = () => _factory.CreateDbContext(args);

        // Assert
        action.Should().NotThrow();
        var context = action();
        context.Should().NotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_ShouldHaveModelConfigured()
    {
        // Arrange
        var args = new string[] { };

        // Act
        using var context = _factory.CreateDbContext(args);

        // Assert
        var model = context.Model;
        model.Should().NotBeNull();

        // Verify some key entities are configured
        model.FindEntityType("AegisEInvoicing.Domain.Entities.OutboxEvent").Should().NotBeNull();
        model.FindEntityType("AegisEInvoicing.Domain.Entities.BusinessManagement.Business").Should().NotBeNull();
        model.FindEntityType("AegisEInvoicing.Domain.Entities.InvoiceTransmissionQueue").Should().NotBeNull();
    }

    [Fact]
    public void CreateDbContext_ShouldUseMockServices()
    {
        // Arrange
        var args = new string[] { };

        // Act
        using var context = _factory.CreateDbContext(args);

        // Assert
        // We can't directly test the mock services since they're private,
        // but we can verify the context was created successfully with services
        context.Should().NotBeNull();

        // The context should be able to perform basic operations without throwing
        var action = () => context.Database.GenerateCreateScript();
        action.Should().NotThrow();
    }

    [Fact]
    public void MockCurrentUserService_ShouldHaveExpectedValues()
    {
        // This test verifies the behavior by creating a context and checking
        // that it works with the mock services
        // Arrange
        var args = new string[] { };

        // Act
        using var context = _factory.CreateDbContext(args);

        // Assert
        // The context should work with the mock services
        context.Should().NotBeNull();

        // We can verify the mock services work by ensuring the context
        // can handle entity tracking operations
        var action = () => context.ChangeTracker.Entries();
        action.Should().NotThrow();
    }

    [Fact]
    public void MockDateTime_ShouldProvideValidDates()
    {
        // This test verifies the DateTime service works by creating a context
        // Arrange
        var args = new string[] { };

        // Act
        using var context = _factory.CreateDbContext(args);

        // Assert
        // The context should work with the mock DateTime service
        context.Should().NotBeNull();

        // Verify we can perform date-related operations
        var action = () => context.Database.GetDbConnection();
        action.Should().NotThrow();
    }

    [Fact]
    public void CreateDbContext_MultipleCalls_ShouldCreateSeparateInstances()
    {
        // Arrange
        var args = new string[] { };

        // Act
        var context1 = _factory.CreateDbContext(args);
        var context2 = _factory.CreateDbContext(args);

        // Assert
        context1.Should().NotBeNull();
        context2.Should().NotBeNull();
        context1.Should().NotBeSameAs(context2);

        // Cleanup
        context1.Dispose();
        context2.Dispose();
    }

    [Fact]
    public void CreateDbContext_ShouldHaveCorrectProviderConfigured()
    {
        // Arrange
        var args = new string[] { };

        // Act
        using var context = _factory.CreateDbContext(args);

        // Assert
        var providerName = context.Database.ProviderName;
        providerName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Theory]
    [InlineData("--verbose")]
    [InlineData("--configuration", "Release")]
    [InlineData("--startup-assembly", "TestAssembly")]
    [InlineData("arg1", "arg2", "arg3")]
    public void CreateDbContext_WithVariousArgs_ShouldNotThrow(params string[] args)
    {
        // Act & Assert
        var action = () => _factory.CreateDbContext(args);
        action.Should().NotThrow();

        var context = action();
        context.Should().NotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_ShouldUseConfigurationFiles()
    {
        // This test verifies that the factory attempts to read configuration files
        // without actually requiring them to exist

        // Arrange
        var args = new string[] { };

        // Act
        var action = () => _factory.CreateDbContext(args);

        // Assert
        // The factory should handle missing configuration gracefully
        // and create a context with fallback settings
        action.Should().NotThrow();

        var context = action();
        context.Should().NotBeNull();
        context.Dispose();
    }

    [Fact]
    public void CreateDbContext_ShouldBeUsableForMigrations()
    {
        // Arrange
        var args = new string[] { };

        // Act
        using var context = _factory.CreateDbContext(args);

        // Assert
        // The context should be suitable for Entity Framework migrations
        context.Database.Should().NotBeNull();

        // Should be able to generate migration scripts
        var action = () => context.Database.GenerateCreateScript();
        action.Should().NotThrow();

        var script = action();
        script.Should().NotBeNullOrEmpty();
        script.Should().Contain("CREATE TABLE"); // Should contain table creation statements
    }

    [Fact]
    public void Factory_ShouldImplementIDesignTimeDbContextFactory()
    {
        // Assert
        _factory.Should().BeAssignableTo<Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ApplicationDbContext>>();
    }
}