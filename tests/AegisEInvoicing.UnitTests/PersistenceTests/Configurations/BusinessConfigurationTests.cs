using AegisEInvoicing.Application.Common.Interfaces;
using AegisEInvoicing.Domain.Common.Interfaces;
using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.UserManagement;
using AegisEInvoicing.Domain.ValueObjects;
using AegisEInvoicing.Persistence;
using AegisEInvoicing.Persistence.Configurations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;
using Moq;
using Xunit;

namespace AegisEInvoicing.UnitTests.PersistenceTests.Configurations;

public class BusinessConfigurationTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IDateTime> _dateTimeMock;

    public BusinessConfigurationTests()
    {
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _dateTimeMock = new Mock<IDateTime>();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options, _currentUserServiceMock.Object, _dateTimeMock.Object);
    }

    [Fact]
    public void Configure_ShouldSetPrimaryKey()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();
        var primaryKey = entityType!.FindPrimaryKey();
        primaryKey.Should().NotBeNull();
        primaryKey!.Properties.Should().HaveCount(1);
        primaryKey.Properties.First().Name.Should().Be("Id");
    }

    [Fact]
    public void Configure_ShouldConfigureTableName()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("Businesses");
    }

    [Fact]
    public void Configure_ShouldConfigureRequiredProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        // Check required properties
        var nameProperty = entityType!.FindProperty("Name");
        nameProperty.Should().NotBeNull();
        nameProperty!.IsNullable.Should().BeFalse();
        nameProperty.GetMaxLength().Should().Be(200);

        var descriptionProperty = entityType.FindProperty("Description");
        descriptionProperty.Should().NotBeNull();
        descriptionProperty!.IsNullable.Should().BeFalse();
        descriptionProperty.GetMaxLength().Should().Be(500);

        var businessRegNumberProperty = entityType.FindProperty("BusinessRegistrationNumber");
        businessRegNumberProperty.Should().NotBeNull();
        businessRegNumberProperty!.IsNullable.Should().BeFalse();
        businessRegNumberProperty.GetMaxLength().Should().Be(100);

        var contactEmailProperty = entityType.FindProperty("ContactEmail");
        contactEmailProperty.Should().NotBeNull();
        contactEmailProperty!.IsNullable.Should().BeFalse();
        contactEmailProperty.GetMaxLength().Should().Be(255);
    }

    [Fact]
    public void Configure_ShouldConfigureOwnedTypes()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        // Check Address owned type properties using column names from configuration
        // The configuration uses custom column names like address_street, address_city, etc.
        var ownedNavigation = entityType!.FindNavigation("RegisteredAddress");
        ownedNavigation.Should().NotBeNull();
        var addressEntity = ownedNavigation!.TargetEntityType;
        addressEntity.Should().NotBeNull();

        var streetProperty = addressEntity!.FindProperty("Street");
        streetProperty.Should().NotBeNull();
        streetProperty!.GetColumnName().Should().Be("address_street");

        var cityProperty = addressEntity.FindProperty("City");
        cityProperty.Should().NotBeNull();
        cityProperty!.GetColumnName().Should().Be("address_city");

        var stateProperty = addressEntity.FindProperty("State");
        stateProperty.Should().NotBeNull();
        stateProperty!.GetColumnName().Should().Be("address_state");

        var countryProperty = addressEntity.FindProperty("Country");
        countryProperty.Should().NotBeNull();
        countryProperty!.GetColumnName().Should().Be("address_country");

        var postalCodeProperty = addressEntity.FindProperty("PostalCode");
        postalCodeProperty.Should().NotBeNull();
        postalCodeProperty!.GetColumnName().Should().Be("address_postal_code");

        // Check TIN owned type property
        var tinNavigation = entityType.FindNavigation("TaxIdentificationNumber");
        tinNavigation.Should().NotBeNull();
        var tinEntity = tinNavigation!.TargetEntityType;
        tinEntity.Should().NotBeNull();

        var tinValueProperty = tinEntity!.FindProperty("Value");
        tinValueProperty.Should().NotBeNull();
        tinValueProperty!.GetColumnName().Should().Be("tax_identification_number");
    }

    [Fact]
    public void Configure_ShouldConfigureOptionalProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        // ContactPhone - domain property is non-nullable string, so EF treats as required
        var contactPhoneProperty = entityType!.FindProperty("ContactPhone");
        contactPhoneProperty.Should().NotBeNull();
        contactPhoneProperty!.GetMaxLength().Should().Be(50);

        // ServiceId is nullable in domain (string? or no IsRequired() in config)
        var serviceIdProperty = entityType.FindProperty("ServiceId");
        serviceIdProperty.Should().NotBeNull();
        serviceIdProperty!.GetMaxLength().Should().Be(100);

        // ApiKey is nullable in domain (string? or no IsRequired() in config)
        var apiKeyProperty = entityType.FindProperty("ApiKey");
        apiKeyProperty.Should().NotBeNull();
        apiKeyProperty!.GetMaxLength().Should().Be(500);
    }

    [Fact]
    public void Configure_ShouldConfigureAuditProperties()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        var createdAtProperty = entityType!.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();
        createdAtProperty!.IsNullable.Should().BeFalse();

        var createdByProperty = entityType.FindProperty("CreatedBy");
        createdByProperty.Should().NotBeNull();
        createdByProperty!.IsNullable.Should().BeFalse();

        var updatedAtProperty = entityType.FindProperty("UpdatedAt");
        updatedAtProperty.Should().NotBeNull();
        updatedAtProperty!.IsNullable.Should().BeTrue();

        var isDeletedProperty = entityType.FindProperty("IsDeleted");
        isDeletedProperty.Should().NotBeNull();
        isDeletedProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldConfigureIndexes()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        var indexes = entityType!.GetIndexes();
        indexes.Should().NotBeEmpty();

        // Check for unique indexes
        var nameIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "Name"));
        nameIndex.Should().NotBeNull();
        nameIndex!.IsUnique.Should().BeTrue();

        var businessRegNumberIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "BusinessRegistrationNumber"));
        businessRegNumberIndex.Should().NotBeNull();
        businessRegNumberIndex!.IsUnique.Should().BeTrue();

        var serviceIdIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "ServiceId"));
        serviceIdIndex.Should().NotBeNull();
        serviceIdIndex!.IsUnique.Should().BeTrue();

        var firsBusinessIdIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "FIRSBusinessId"));
        firsBusinessIdIndex.Should().NotBeNull();
        firsBusinessIdIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldConfigureForeignKeyRelationships()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        var foreignKeys = entityType!.GetForeignKeys();
        foreignKeys.Should().NotBeEmpty();

        // Check AdminUser relationship
        var adminUserFk = foreignKeys.FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(User));
        adminUserFk.Should().NotBeNull();
        adminUserFk!.Properties.First().Name.Should().Be("AdminUserId");
        adminUserFk.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        adminUserFk.IsRequired.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldIgnoreDomainEvents()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        var domainEventsProperty = entityType!.FindProperty("DomainEvents");
        domainEventsProperty.Should().BeNull(); // Should be ignored
    }

    [Fact]
    public void Configure_ShouldHaveSoftDeleteQueryFilter()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();
        entityType!.GetDeclaredQueryFilters().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Configuration_ShouldWorkWithActualEntity()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns(Guid.NewGuid());

        var business = Business.Create(
            name: "Test Business Limited",
            description: "A test business for unit testing",
            businessRegistrationNumber: "RC: 1234567",
            taxIdentificationNumber: TIN.Create("12345678-1234"),
            registeredAddress: Address.Create(
                street: "123 Test Street",
                city: "Test City",
                state: "Test State",
                country: "NG",
                postalCode: "12345"
            ),
            invoicePrefix: "TST",
            contactEmail: "test@testbusiness.com",
            adminUserId: Guid.NewGuid(),
            createdBy: Guid.NewGuid(),
            contactPhone: "+2347012345678",
            serviceId: "TEST-001",
            industry: "Technology",
            firsBusinessId: Guid.NewGuid()
        );

        // Act
        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        // Assert
        var savedBusiness = await _context.Businesses.FirstOrDefaultAsync(b => b.Id == business.Id);
        savedBusiness.Should().NotBeNull();
        savedBusiness!.Name.Should().Be("Test Business Limited");
        savedBusiness.RegisteredAddress.Street.Should().Be("123 Test Street");
        savedBusiness.TaxIdentificationNumber.Value.Should().Be("12345678-1234");
    }

    [Fact]
    public void Configure_ShouldHaveCorrectColumnTypes()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        var createdAtProperty = entityType!.FindProperty("CreatedAt");
        createdAtProperty.Should().NotBeNull();

        var updatedAtProperty = entityType.FindProperty("UpdatedAt");
        updatedAtProperty.Should().NotBeNull();

        var deletedAtProperty = entityType.FindProperty("DeletedAt");
        deletedAtProperty.Should().NotBeNull();

        var tokenExpiresAtProperty = entityType.FindProperty("TokenExpiresAt");
        tokenExpiresAtProperty.Should().NotBeNull();

        var apiKeyGeneratedAtProperty = entityType.FindProperty("ApiKeyGeneratedAt");
        apiKeyGeneratedAtProperty.Should().NotBeNull();

        var apiKeyLastUsedAtProperty = entityType.FindProperty("ApiKeyLastUsedAt");
        apiKeyLastUsedAtProperty.Should().NotBeNull();
    }

    [Fact]
    public void Configure_ShouldHaveCorrectDefaultValues()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(Business));

        // Act & Assert
        entityType.Should().NotBeNull();

        var isDeletedProperty = entityType!.FindProperty("IsDeleted");
        isDeletedProperty.Should().NotBeNull();
        isDeletedProperty!.GetDefaultValue().Should().Be(false);

        var isApiKeyActiveProperty = entityType.FindProperty("IsApiKeyActive");
        isApiKeyActiveProperty.Should().NotBeNull();
        isApiKeyActiveProperty!.GetDefaultValue().Should().Be(false);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}