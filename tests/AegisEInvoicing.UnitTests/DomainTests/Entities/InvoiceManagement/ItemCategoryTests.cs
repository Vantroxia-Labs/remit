using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.InvoiceManagement;

public class ItemCategoryTests
{
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly string _validName = "Electronics";
    private readonly string _validDescription = "Electronic devices and components";

    [Fact]
    public void Create_WithValidParameters_ShouldCreateItemCategory()
    {
        // Act
        var itemCategory = ItemCategory.Create(_validName, _validDescription, _businessId);

        // Assert
        itemCategory.Should().NotBeNull();
        itemCategory.Id.Should().NotBeEmpty();
        itemCategory.Name.Should().Be(_validName);
        itemCategory.Description.Should().Be(_validDescription);
        itemCategory.BusinessID.Should().Be(_businessId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var action = () => ItemCategory.Create(invalidName, _validDescription, _businessId);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Category name is required*")
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Create_WithEmptyBusinessId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => ItemCategory.Create(_validName, _validDescription, Guid.Empty);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Business ID cannot be empty*")
            .And.ParamName.Should().Be("businessId");
    }

    [Fact]
    public void Create_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longName = new string('A', 101); // Over 100 characters

        // Act & Assert
        var action = () => ItemCategory.Create(longName, _validDescription, _businessId);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Category name cannot exceed 100 characters*")
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Create_WithDescriptionTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var longDescription = new string('A', 501); // Over 500 characters

        // Act & Assert
        var action = () => ItemCategory.Create(_validName, longDescription, _businessId);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Category description cannot exceed 500 characters*")
            .And.ParamName.Should().Be("description");
    }

    [Fact]
    public void Create_WithWhitespaceDescription_ShouldStoreWhitespace()
    {
        // Act
        var itemCategory = ItemCategory.Create(_validName, " ", _businessId);

        // Assert
        // The entity stores the description as provided (whitespace is not converted to null)
        itemCategory.Description.Should().Be(" ");
        // HasDescription checks if description is not null or whitespace
        itemCategory.HasDescription().Should().BeFalse();
    }

    [Fact]
    public void CreateWithBusiness_WithValidBusiness_ShouldCreateItemCategory()
    {
        // Arrange
        var business = CreateMockBusiness();

        // Act
        var itemCategory = ItemCategory.CreateWithBusiness(_validName, _validDescription, business);

        // Assert
        itemCategory.Should().NotBeNull();
        itemCategory.Name.Should().Be(_validName);
        itemCategory.BusinessID.Should().Be(business.Id);
        itemCategory.Business.Should().Be(business);
    }

    [Fact]
    public void CreateWithBusiness_WithNullBusiness_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => ItemCategory.CreateWithBusiness(_validName, _validDescription, null!);
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("business");
    }

    [Fact]
    public void UpdateName_WithValidName_ShouldUpdateName()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var newName = "Updated Electronics";

        // Act
        itemCategory.UpdateName(newName);

        // Assert
        itemCategory.Name.Should().Be(newName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateName_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        var action = () => itemCategory.UpdateName(invalidName);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Category name cannot be empty*")
            .And.ParamName.Should().Be("newName");
    }

    [Fact]
    public void UpdateName_WithNameTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var longName = new string('A', 101);

        // Act & Assert
        var action = () => itemCategory.UpdateName(longName);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Category name cannot exceed 100 characters*");
    }

    [Fact]
    public void UpdateName_WithSameName_ShouldNotChange()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var originalName = itemCategory.Name;

        // Act
        itemCategory.UpdateName(originalName);

        // Assert
        itemCategory.Name.Should().Be(originalName);
    }

    [Fact]
    public void UpdateDescription_WithValidDescription_ShouldUpdateDescription()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var newDescription = "Updated description for electronics";

        // Act
        itemCategory.UpdateDescription(newDescription);

        // Assert
        itemCategory.Description.Should().Be(newDescription);
    }

    [Fact]
    public void UpdateDescription_WithNullDescription_ShouldSetEmptyString()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act
        itemCategory.UpdateDescription(null);

        // Assert
        itemCategory.Description.Should().Be(string.Empty);
    }

    [Fact]
    public void UpdateDescription_WithDescriptionTooLong_ShouldThrowArgumentException()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var longDescription = new string('A', 501);

        // Act & Assert
        var action = () => itemCategory.UpdateDescription(longDescription);
        action.Should().Throw<ArgumentException>()
            .WithMessage("Category description cannot exceed 500 characters*");
    }

    [Fact]
    public void UpdateDetails_WithValidParameters_ShouldUpdateBothNameAndDescription()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var newName = "Updated Electronics";
        var newDescription = "Updated description";

        // Act
        itemCategory.UpdateDetails(newName, newDescription);

        // Assert
        itemCategory.Name.Should().Be(newName);
        itemCategory.Description.Should().Be(newDescription);
    }

    [Fact]
    public void TransferToBusiness_WithValidBusiness_ShouldUpdateBusiness()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var newBusiness = CreateMockBusiness();

        // Act
        itemCategory.TransferToBusiness(newBusiness);

        // Assert
        itemCategory.BusinessID.Should().Be(newBusiness.Id);
        itemCategory.Business.Should().Be(newBusiness);
    }

    [Fact]
    public void TransferToBusiness_WithNullBusiness_ShouldThrowArgumentNullException()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        var action = () => itemCategory.TransferToBusiness(null!);
        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("newBusiness");
    }

    [Fact]
    public void TransferToBusiness_WithSameBusiness_ShouldNotChange()
    {
        // Arrange
        var business = CreateMockBusiness();
        var itemCategory = ItemCategory.CreateWithBusiness(_validName, _validDescription, business);
        var originalBusinessId = itemCategory.BusinessID;

        // Act
        itemCategory.TransferToBusiness(business);

        // Assert
        itemCategory.BusinessID.Should().Be(originalBusinessId);
    }

    [Fact]
    public void BelongsToBusiness_WithMatchingBusinessId_ShouldReturnTrue()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        itemCategory.BelongsToBusiness(_businessId).Should().BeTrue();
    }

    [Fact]
    public void BelongsToBusiness_WithDifferentBusinessId_ShouldReturnFalse()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();
        var differentBusinessId = Guid.NewGuid();

        // Act & Assert
        itemCategory.BelongsToBusiness(differentBusinessId).Should().BeFalse();
    }

    [Theory]
    [InlineData("Electronics", true)]
    [InlineData("electronics", true)]
    [InlineData("ELECTRONICS", true)]
    [InlineData("Computers", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasName_WithVariousNames_ShouldReturnExpectedResult(string? nameToCheck, bool expectedResult)
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        itemCategory.HasName(nameToCheck).Should().Be(expectedResult);
    }

    [Fact]
    public void HasDescription_WithDescription_ShouldReturnTrue()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        itemCategory.HasDescription().Should().BeTrue();
    }

    [Fact]
    public void HasDescription_WithEmptyDescription_ShouldReturnFalse()
    {
        // Arrange
        var itemCategory = ItemCategory.Create(_validName, "", _businessId);

        // Act & Assert
        itemCategory.HasDescription().Should().BeFalse();
    }

    [Fact]
    public void GetDisplayName_WithBusiness_ShouldReturnNameWithBusinessName()
    {
        // Arrange
        var business = CreateMockBusiness();
        var itemCategory = ItemCategory.CreateWithBusiness(_validName, _validDescription, business);

        // Act
        var displayName = itemCategory.GetDisplayName();

        // Assert
        displayName.Should().Be($"{_validName} ({business.Name})");
    }

    [Fact]
    public void GetDisplayName_WithoutBusiness_ShouldReturnNameOnly()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act
        var displayName = itemCategory.GetDisplayName();

        // Assert
        displayName.Should().Be(_validName);
    }

    [Fact]
    public void GetFormattedDescription_WithDescription_ShouldReturnDescription()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act
        var formattedDescription = itemCategory.GetFormattedDescription();

        // Assert
        formattedDescription.Should().Be(_validDescription);
    }

    [Fact]
    public void GetFormattedDescription_WithoutDescription_ShouldReturnDefaultMessage()
    {
        // Arrange
        var itemCategory = ItemCategory.Create(_validName, "", _businessId);

        // Act
        var formattedDescription = itemCategory.GetFormattedDescription();

        // Assert
        formattedDescription.Should().Be("No description available");
    }

    [Fact]
    public void IsComplete_WithValidNameAndBusinessId_ShouldReturnTrue()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        itemCategory.IsComplete().Should().BeTrue();
    }

    [Fact]
    public void CanBeDeleted_ShouldReturnTrue()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        itemCategory.CanBeDeleted().Should().BeTrue();
    }

    [Fact]
    public void CanBeArchived_ShouldReturnTrue()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        itemCategory.CanBeArchived().Should().BeTrue();
    }

    [Fact]
    public void Archive_WhenCanBeArchived_ShouldNotThrow()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        var action = () => itemCategory.Archive();
        action.Should().NotThrow();
    }

    [Fact]
    public void CompareTo_WithDifferentCategory_ShouldReturnComparison()
    {
        // Arrange
        var category1 = ItemCategory.Create("Apple", "Fruit", _businessId);
        var category2 = ItemCategory.Create("Banana", "Fruit", _businessId);

        // Act
        var result = category1.CompareTo(category2);

        // Assert
        result.Should().BeLessThan(0); // "Apple" comes before "Banana"
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturnOne()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act
        var result = itemCategory.CompareTo(null);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void IsSimilarTo_WithSameCategoryName_ShouldReturnTrue()
    {
        // Arrange
        var category1 = ItemCategory.Create("Electronics", "Description 1", _businessId);
        var category2 = ItemCategory.Create("Electronics", "Description 2", _businessId);

        // Act & Assert
        category1.IsSimilarTo(category2).Should().BeTrue();
    }

    [Fact]
    public void IsSimilarTo_WithDifferentBusinessId_ShouldReturnFalse()
    {
        // Arrange
        var category1 = ItemCategory.Create("Electronics", "Description", _businessId);
        var category2 = ItemCategory.Create("Electronics", "Description", Guid.NewGuid());

        // Act & Assert
        category1.IsSimilarTo(category2).Should().BeFalse();
    }

    [Fact]
    public void IsSimilarTo_WithNull_ShouldReturnFalse()
    {
        // Arrange
        var itemCategory = CreateTestItemCategory();

        // Act & Assert
        itemCategory.IsSimilarTo(null).Should().BeFalse();
    }

    private ItemCategory CreateTestItemCategory()
    {
        return ItemCategory.Create(_validName, _validDescription, _businessId);
    }

    private Business CreateMockBusiness()
    {
        // Create a mock business object for testing
        var business = Business.Create(
                        name: "Lagos Tech Solutions Limited",
                        description: "A leading technology company specializing in fintech solutions and software development",
                        businessRegistrationNumber: "RC: 1234567", // CAC registration format
                        taxIdentificationNumber: TIN.Create("12345678-1234"), // Nigerian TIN format
                        registeredAddress: Address.Create(
                            street: "123 Adeola Odeku Street",
                            city: "Victoria Island",
                            state: "Lagos",
                            country: "NG",
                            postalCode: "101241"
                        ),
                        invoicePrefix: "LTS",
                        contactEmail: "info@lagostechnologies.com.ng",
                        adminUserId: Guid.NewGuid(),
                        createdBy: Guid.NewGuid(),
                        contactPhone: "+2347012345678", // Nigerian phone format
                        serviceId: "FINTECH-001",
                        industry: "Financial Technology",
                        firsBusinessId: Guid.NewGuid()
        );
        return business;
    }
}