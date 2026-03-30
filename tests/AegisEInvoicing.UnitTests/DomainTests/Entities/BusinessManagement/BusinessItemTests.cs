using AegisEInvoicing.Domain.Entities.BusinessManagement;
using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.BusinessManagement;

public class BusinessItemTests
{
    private readonly Guid _businessId = Guid.NewGuid();
    private readonly Guid _itemCategoryId = Guid.NewGuid();
    private readonly ServiceCode _validServiceCode;
    private readonly TaxCategory _validTaxCategory;

    public BusinessItemTests()
    {
        _validServiceCode = ServiceCode.Create("SVC001", "Consulting Services");
        _validTaxCategory = TaxCategory.Create("VAT", 7.5m);
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateBusinessItem()
    {
        // Arrange
        var name = "Laptop Computer";
        var description = "High-performance business laptop";
        var unitPrice = 150000.0m;

        // Act
        var businessItem = BusinessItem.Create(
            _businessId,
            name,
            _validServiceCode,
            _validTaxCategory,
            _itemCategoryId,
            description,
            unitPrice);

        // Assert
        businessItem.Should().NotBeNull();
        businessItem.BusinessID.Should().Be(_businessId);
        businessItem.Name.Should().Be(name);
        businessItem.ServiceCode.Should().Be(_validServiceCode);
        businessItem.TaxCategory.Should().Be(_validTaxCategory);
        businessItem.ItemCategoryId.Should().Be(_itemCategoryId);
        businessItem.ItemDescription.Should().Be(description);
        businessItem.UnitPrice.Should().Be(unitPrice);
        businessItem.ItemId.Should().NotBeNullOrEmpty();
        businessItem.InvoiceItems.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Act & Assert
        var action = () => BusinessItem.Create(
            _businessId,
            invalidName!,
            _validServiceCode,
            _validTaxCategory,
            _itemCategoryId,
            "Valid description",
            100.0m);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty.*")
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Create_WithNullServiceCode_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => BusinessItem.Create(
            _businessId,
            "Valid Name",
            null!,
            _validTaxCategory,
            _itemCategoryId,
            "Valid description",
            100.0m);

        action.Should().Throw<ArgumentException>()
            .WithMessage("ServiceCode cannot be empty.*")
            .And.ParamName.Should().Be("serviceCode");
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_ShouldThrowArgumentOutOfRangeException()
    {
        // Act & Assert
        var action = () => BusinessItem.Create(
            _businessId,
            "Valid Name",
            _validServiceCode,
            _validTaxCategory,
            _itemCategoryId,
            "Valid description",
            -100.0m);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("UnitPrice cannot be negative.*")
            .And.ParamName.Should().Be("unitPrice");
    }

    [Fact]
    public void Create_WithZeroUnitPrice_ShouldCreateBusinessItem()
    {
        // Act
        var businessItem = BusinessItem.Create(
            _businessId,
            "Free Item",
            _validServiceCode,
            _validTaxCategory,
            _itemCategoryId,
            "Free promotional item",
            0.0m);

        // Assert
        businessItem.UnitPrice.Should().Be(0.0m);
    }

    [Fact]
    public void Update_WithValidParameters_ShouldUpdateBusinessItem()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();
        var newName = "Updated Item Name";
        var newServiceCode = ServiceCode.Create("SVC002", "Updated Service");
        var newTaxCategory = TaxCategory.Create("NHIL", 2.5m);
        var newCategoryId = Guid.NewGuid();
        var newDescription = "Updated description";

        // Act
        businessItem.Update(
            newName,
            newServiceCode,
            newTaxCategory,
            newCategoryId,
            newDescription);

        // Assert
        businessItem.Name.Should().Be(newName);
        businessItem.ServiceCode.Should().Be(newServiceCode);
        businessItem.TaxCategory.Should().Be(newTaxCategory);
        businessItem.ItemCategoryId.Should().Be(newCategoryId);
        businessItem.ItemDescription.Should().Be(newDescription);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Update_WithInvalidName_ShouldThrowArgumentException(string? invalidName)
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        var action = () => businessItem.Update(
            invalidName!,
            _validServiceCode,
            _validTaxCategory,
            _itemCategoryId,
            "Valid description");

        action.Should().Throw<ArgumentException>()
            .WithMessage("Name cannot be empty.*")
            .And.ParamName.Should().Be("name");
    }

    [Fact]
    public void Update_WithNullServiceCode_ShouldThrowArgumentException()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        var action = () => businessItem.Update(
            "Valid Name",
            null!,
            _validTaxCategory,
            _itemCategoryId,
            "Valid description");

        action.Should().Throw<ArgumentException>()
            .WithMessage("ServiceCode cannot be empty.*")
            .And.ParamName.Should().Be("serviceCode");
    }

    [Fact]
    public void ProposePrice_WithValidNewPrice_ShouldCreatePriceHistory()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();
        var newPrice = 200000.0m;

        // Act
        var priceHistory = businessItem.ProposePrice(newPrice, "Test price change");

        // Assert
        priceHistory.Should().NotBeNull();
        priceHistory.OldPrice.Should().Be(businessItem.UnitPrice);
        priceHistory.NewPrice.Should().Be(newPrice);
        priceHistory.Status.Should().Be(AegisEInvoicing.Domain.Enums.ApprovalStatus.Pending);
        priceHistory.Comments.Should().Be("Test price change");
        businessItem.HasPendingPriceChange.Should().BeTrue();
    }

    [Fact]
    public void ProposePrice_WithNegativePrice_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        var action = () => businessItem.ProposePrice(-100.0m);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("New price cannot be negative.*")
            .And.ParamName.Should().Be("newPrice");
    }

    [Fact]
    public void ProposePrice_WithSamePrice_ShouldThrowArgumentException()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        var action = () => businessItem.ProposePrice(businessItem.UnitPrice);

        action.Should().Throw<ArgumentException>()
            .WithMessage("New price must be different from current price.*")
            .And.ParamName.Should().Be("newPrice");
    }

    [Fact]
    public void UpdatePriceFromErp_WithValidPrice_ShouldUpdateUnitPrice()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();
        var newPrice = 250000.0m;

        // Act
        businessItem.UpdatePriceFromErp(newPrice);

        // Assert
        businessItem.UnitPrice.Should().Be(newPrice);
    }

    [Fact]
    public void UpdatePriceFromErp_WithNegativePrice_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        var action = () => businessItem.UpdatePriceFromErp(-100.0m);

        action.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("UnitPrice cannot be negative.*")
            .And.ParamName.Should().Be("unitPrice");
    }

    [Fact]
    public void UpdateDescription_WithValidDescription_ShouldUpdateDescription()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();
        var newDescription = "Updated item description with more details";

        // Act
        businessItem.UpdateDescription(newDescription);

        // Assert
        businessItem.ItemDescription.Should().Be(newDescription);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void UpdateDescription_WithInvalidDescription_ShouldThrowArgumentException(string? invalidDescription)
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        var action = () => businessItem.UpdateDescription(invalidDescription!);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Description cannot be empty.*")
            .And.ParamName.Should().Be("description");
    }

    [Fact]
    public void UpdateCategory_WithValidCategoryId_ShouldUpdateCategory()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();
        var newCategoryId = Guid.NewGuid();

        // Act
        businessItem.UpdateCategory(newCategoryId);

        // Assert
        businessItem.ItemCategoryId.Should().Be(newCategoryId);
    }

    [Fact]
    public void UpdateCategory_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        var action = () => businessItem.UpdateCategory(Guid.Empty);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Category cannot be empty.*")
            .And.ParamName.Should().Be("categoryId");
    }

    [Fact]
    public void InvoiceItems_ShouldReturnReadOnlyCollection()
    {
        // Arrange
        var businessItem = CreateTestBusinessItem();

        // Act & Assert
        businessItem.InvoiceItems.Should().NotBeNull();
        businessItem.InvoiceItems.Should().BeEmpty();
        businessItem.InvoiceItems.Should().BeAssignableTo<IReadOnlyCollection<AegisEInvoicing.Domain.Entities.InvoiceManagement.InvoiceItem>>();
    }

    private BusinessItem CreateTestBusinessItem()
    {
        return BusinessItem.Create(
            _businessId,
            "Test Item",
            _validServiceCode,
            _validTaxCategory,
            _itemCategoryId,
            "Test item description",
            100000.0m);
    }
}