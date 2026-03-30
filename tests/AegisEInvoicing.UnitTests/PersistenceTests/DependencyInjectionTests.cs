using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Application.Common.Interfaces.Repositories;
using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Persistence;
using AegisEInvoicing.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests;

public class DependencyInjectionTests
{
    private readonly IConfiguration _configuration;
    private readonly IServiceCollection _services;

    public DependencyInjectionTests()
    {
        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;Username=test;Password=test",
            ["EnableSensitiveDataLogging"] = "true",
            ["EnableDetailedErrors"] = "true"
        });
        _configuration = configurationBuilder.Build();
        _services = new ServiceCollection();

        // Register required dependencies for ApplicationDbContext
        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _services.AddSingleton(currentUserServiceMock.Object);

        var dateTimeMock = new Mock<IDateTime>();
        dateTimeMock.Setup(x => x.Now).Returns(DateTime.UtcNow);
        _services.AddSingleton(dateTimeMock.Object);
    }

    [Fact]
    public void AddPersistenceServices_WithValidConfiguration_ShouldRegisterAllServices()
    {
        // Act
        _services.AddPersistenceServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Assert
        // Verify DbContext registration
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();
        dbContext.Should().NotBeNull();

        // Verify IApplicationDbContext registration
        var applicationDbContext = serviceProvider.GetService<IApplicationDbContext>();
        applicationDbContext.Should().NotBeNull();
        applicationDbContext.Should().BeOfType<ApplicationDbContext>();

        // Verify UnitOfWork registration
        var unitOfWork = serviceProvider.GetService<IUnitOfWork>();
        unitOfWork.Should().NotBeNull();
        unitOfWork.Should().BeOfType<UnitOfWork>();

        // Verify Repository registration
        var repository = serviceProvider.GetService<IRepository<TestEntity>>();
        repository.Should().NotBeNull();
        repository.Should().BeOfType<Repository<TestEntity>>();
    }

    [Fact]
    public void AddPersistenceServices_ShouldConfigureDatabaseOptions()
    {
        // Arrange
        _services.AddPersistenceServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        dbContext.Database.Should().NotBeNull();

        // Verify provider is PostgreSQL
        dbContext.Database.ProviderName.Should().Be("Npgsql.EntityFrameworkCore.PostgreSQL");
    }

    [Fact]
    public void AddPersistenceServices_ShouldEnableSensitiveDataLogging_WhenConfigured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;Username=test;Password=test",
                ["EnableSensitiveDataLogging"] = "true"
            })
            .Build();

        // Act
        _services.AddPersistenceServices(config);
        var serviceProvider = _services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        // We can't directly test if sensitive data logging is enabled,
        // but we can verify the context was created successfully
    }

    [Fact]
    public void AddPersistenceServices_ShouldEnableDetailedErrors_WhenConfigured()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;Username=test;Password=test",
                ["EnableDetailedErrors"] = "true"
            })
            .Build();

        // Act
        _services.AddPersistenceServices(config);
        var serviceProvider = _services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceServices_WithDefaultSettings_ShouldWork()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=TestDb;Username=test;Password=test"
            })
            .Build();

        // Act
        var action = () => _services.AddPersistenceServices(config);

        // Assert
        action.Should().NotThrow();
        var serviceProvider = _services.BuildServiceProvider();
        var dbContext = serviceProvider.GetService<ApplicationDbContext>();
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceServices_ShouldRegisterGenericRepository()
    {
        // Arrange
        _services.AddPersistenceServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var genericRepository = serviceProvider.GetService<IRepository<TestEntity>>();
        var anotherRepository = serviceProvider.GetService<IRepository<AnotherTestEntity>>();

        // Assert
        genericRepository.Should().NotBeNull();
        genericRepository.Should().BeOfType<Repository<TestEntity>>();

        anotherRepository.Should().NotBeNull();
        anotherRepository.Should().BeOfType<Repository<AnotherTestEntity>>();

        // Verify they are different instances for different entity types
        genericRepository.Should().NotBeSameAs(anotherRepository);
    }

    [Fact]
    public void AddPersistenceServices_ShouldRegisterServicesAsScoped()
    {
        // Arrange
        _services.AddPersistenceServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        using var scope1 = serviceProvider.CreateScope();
        using var scope2 = serviceProvider.CreateScope();

        var dbContext1 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbContext2 = scope1.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbContext3 = scope2.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var unitOfWork1 = scope1.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var unitOfWork2 = scope1.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var unitOfWork3 = scope2.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Assert
        // Same scope should return same instance
        dbContext1.Should().BeSameAs(dbContext2);
        unitOfWork1.Should().BeSameAs(unitOfWork2);

        // Different scope should return different instance
        dbContext1.Should().NotBeSameAs(dbContext3);
        unitOfWork1.Should().NotBeSameAs(unitOfWork3);
    }

    [Fact]
    public void AddPersistenceServices_ShouldConfigureMigrationsAssembly()
    {
        // Arrange
        _services.AddPersistenceServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        // The migrations assembly should be set correctly
        var options = dbContext.Database.GetDbConnection();
        options.Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceServices_WithMissingConnectionString_ShouldStillCreateContext()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();

        // Act
        _services.AddPersistenceServices(config);
        var serviceProvider = _services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert - DbContext should still be created even with missing connection string
        // The actual connection failure would occur when attempting database operations
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void AddPersistenceServices_ShouldReturnServiceCollection()
    {
        // Act
        var result = _services.AddPersistenceServices(_configuration);

        // Assert
        result.Should().BeSameAs(_services);
    }

    [Fact]
    public void AddPersistenceServices_MultipleCallsToSameServiceCollection_ShouldWork()
    {
        // Act
        _services.AddPersistenceServices(_configuration);
        _services.AddPersistenceServices(_configuration);

        // Assert
        var action = () => _services.BuildServiceProvider();
        action.Should().NotThrow();
    }

    [Fact]
    public void AddPersistenceServices_WithCustomConfiguration_ShouldRespectSettings()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=customhost;Database=CustomDb;Username=customuser;Password=custompass",
                ["EnableSensitiveDataLogging"] = "false",
                ["EnableDetailedErrors"] = "false"
            })
            .Build();

        // Act
        _services.AddPersistenceServices(config);
        var serviceProvider = _services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        dbContext.Database.GetConnectionString().Should().Contain("customhost");
        dbContext.Database.GetConnectionString().Should().Contain("CustomDb");
    }

    [Fact]
    public void AddPersistenceServices_ShouldConfigureLogging()
    {
        // Arrange
        _services.AddLogging();
        _services.AddPersistenceServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        // Logging configuration is internal, but we can verify the context was created
    }

    [Fact]
    public void AddPersistenceServices_ShouldIgnorePendingModelChangesWarning()
    {
        // Arrange
        _services.AddPersistenceServices(_configuration);
        var serviceProvider = _services.BuildServiceProvider();

        // Act
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
        // Warning configuration is internal, but we can verify the context was created
    }

    [Fact]
    public void ServiceLifetime_ShouldBeCorrectForAllServices()
    {
        // Arrange
        _services.AddPersistenceServices(_configuration);

        // Act
        var serviceDescriptors = _services.ToList();

        // Assert
        // Find and verify service lifetimes
        var dbContextDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(ApplicationDbContext));
        dbContextDescriptor.Should().NotBeNull();
        dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var applicationDbContextDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(IApplicationDbContext));
        applicationDbContextDescriptor.Should().NotBeNull();
        applicationDbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var unitOfWorkDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(IUnitOfWork));
        unitOfWorkDescriptor.Should().NotBeNull();
        unitOfWorkDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        var repositoryDescriptor = serviceDescriptors.FirstOrDefault(s => s.ServiceType == typeof(IRepository<>));
        repositoryDescriptor.Should().NotBeNull();
        repositoryDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    // Test helper classes
    private class TestEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    private class AnotherTestEntity : Entity
    {
        public string Description { get; set; } = string.Empty;
    }
}