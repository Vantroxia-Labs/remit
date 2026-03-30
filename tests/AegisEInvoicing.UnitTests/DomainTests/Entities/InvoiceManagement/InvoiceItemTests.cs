using AegisEInvoicing.Domain.Entities.InvoiceManagement;
using AegisEInvoicing.Domain.Enums;
using AegisEInvoicing.Domain.Exceptions;
using AegisEInvoicing.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace AegisEInvoicing.UnitTests.DomainTests.Entities.InvoiceManagement;

public class InvoiceItemTests
{
    private readonly Guid _businessItemId = Guid.NewGuid();
    private readonly Guid _invoiceId = Guid.NewGuid();
    private readonly decimal _validQuantity = 5;
    private readonly decimal _validUnitPrice = 100.50m;
    private readonly DiscountFee _validDiscountFee;
    private readonly AdditionalFee _validAdditionalFee;

    public InvoiceItemTests()
    {
        _validDiscountFee = DiscountFee.Create(10.0m, FeeStandardUnit.Percent);
        _validAdditionalFee = AdditionalFee.Create(50.0m, FeeStandardUnit.NGN);
    }

    [Fact]
    public void Create_WithValidParameters_ShouldCreateInvoiceItem()
    {
        // Act
        var invoiceItem = InvoiceItem.Create(
            _businessItemId,
            _invoiceId,
            _validQuantity,
            _validUnitPrice,
            _validDiscountFee,
            _validAdditionalFee);

        // Assert
        invoiceItem.Should().NotBeNull();
        invoiceItem.Id.Should().NotBeEmpty();
        invoiceItem.BusinessItemId.Should().Be(_businessItemId);
        invoiceItem.InvoiceId.Should().Be(_invoiceId);
        invoiceItem.Quantity.Should().Be(_validQuantity);
        invoiceItem.UnitPriceSnapshot.Should().Be(_validUnitPrice);
        invoiceItem.DiscountFee.Should().Be(_validDiscountFee);
        invoiceItem.AdditionalFee.Should().Be(_validAdditionalFee);
    }

    [Fact]
    public void Create_WithNullOptionalParameters_ShouldCreateInvoiceItem()
    {
        // Act
        var invoiceItem = InvoiceItem.Create(
            _businessItemId,
            _invoiceId,
            _validQuantity,
            _validUnitPrice,
            null,
            null);

        // Assert
        invoiceItem.Should().NotBeNull();
        invoiceItem.BusinessItemId.Should().Be(_businessItemId);
        invoiceItem.InvoiceId.Should().Be(_invoiceId);
        invoiceItem.Quantity.Should().Be(_validQuantity);
        invoiceItem.UnitPriceSnapshot.Should().Be(_validUnitPrice);
        invoiceItem.DiscountFee.Should().BeNull();
        invoiceItem.AdditionalFee.Should().BeNull();
    }

    [Fact]
    public void Create_WithEmptyBusinessItemId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => InvoiceItem.Create(
            Guid.Empty,
            _invoiceId,
            _validQuantity,
            _validUnitPrice,
            null,
            null);

        action.Should().Throw<ArgumentException>()
            .WithMessage("BusinessItemId cannot be empty*")
            .And.ParamName.Should().Be("businessItemId");
    }

    [Fact]
    public void Create_WithEmptyInvoiceId_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => InvoiceItem.Create(
            _businessItemId,
            Guid.Empty,
            _validQuantity,
            _validUnitPrice,
            null,
            null);

        action.Should().Throw<ArgumentException>()
            .WithMessage("InvoiceId cannot be empty*")
            .And.ParamName.Should().Be("invoiceId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithInvalidQuantity_ShouldThrowArgumentException(int invalidQuantity)
    {
        // Act & Assert
        var action = () => InvoiceItem.Create(
            _businessItemId,
            _invoiceId,
            invalidQuantity,
            _validUnitPrice,
            null,
            null);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Quantity must be greater than zero*")
            .And.ParamName.Should().Be("quantity");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Create_WithNegativeUnitPriceSnapshot_ShouldThrowArgumentException(decimal negativePrice)
    {
        // Act & Assert
        var action = () => InvoiceItem.Create(
            _businessItemId,
            _invoiceId,
            _validQuantity,
            negativePrice,
            null,
            null);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Unit price snapshot cannot be negative*")
            .And.ParamName.Should().Be("unitPriceSnapshot");
    }

    [Fact]
    public void WithDiscount_WithValidDiscountFee_ShouldSetDiscountFee()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();
        var discountFee = DiscountFee.Create(15.0m, FeeStandardUnit.Percent);

        // Act
        var result = invoiceItem.WithDiscount(discountFee);

        // Assert
        result.Should().BeSameAs(invoiceItem); // Should return same instance
        invoiceItem.DiscountFee.Should().Be(discountFee);
    }

    [Fact]
    public void WithDiscount_WithNullDiscountFee_ShouldThrowArgumentNullException()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();

        // Act & Assert
        var action = () => invoiceItem.WithDiscount(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithAdditionalFee_WithValidAdditionalFee_ShouldSetAdditionalFee()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();
        var additionalFee = AdditionalFee.Create(25.0m, FeeStandardUnit.NGN);

        // Act
        var result = invoiceItem.WithAdditionalFee(additionalFee);

        // Assert
        result.Should().BeSameAs(invoiceItem); // Should return same instance
        invoiceItem.AdditionalFee.Should().Be(additionalFee);
    }

    [Fact]
    public void WithAdditionalFee_WithNullAdditionalFee_ShouldThrowArgumentNullException()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();

        // Act & Assert
        var action = () => invoiceItem.WithAdditionalFee(null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void UpdateQuantity_WithValidQuantity_ShouldUpdateQuantity()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();
        var newQuantity = 10;

        // Act
        invoiceItem.UpdateQuantity(newQuantity);

        // Assert
        invoiceItem.Quantity.Should().Be(newQuantity);
    }
   

    [Fact]
    public void UpdateDiscountFee_WithValidDiscountFee_ShouldUpdateDiscountFee()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();
        var newDiscountFee = DiscountFee.Create(20.0m, FeeStandardUnit.Percent);

        // Act
        invoiceItem.UpdateDiscountFee(newDiscountFee);

        // Assert
        invoiceItem.DiscountFee.Should().Be(newDiscountFee);
    }

    [Fact]
    public void UpdateDiscountFee_WithNullDiscountFee_ShouldSetToNull()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();

        // Act
        invoiceItem.UpdateDiscountFee(null);

        // Assert
        invoiceItem.DiscountFee.Should().BeNull();
    }

    [Fact]
    public void UpdateAdditionalFee_WithValidAdditionalFee_ShouldUpdateAdditionalFee()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();
        var newAdditionalFee = AdditionalFee.Create(75.0m, FeeStandardUnit.NGN);

        // Act
        invoiceItem.UpdateAdditionalFee(newAdditionalFee);

        // Assert
        invoiceItem.AdditionalFee.Should().Be(newAdditionalFee);
    }

    [Fact]
    public void UpdateAdditionalFee_WithNullAdditionalFee_ShouldSetToNull()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();

        // Act
        invoiceItem.UpdateAdditionalFee(null);

        // Assert
        invoiceItem.AdditionalFee.Should().BeNull();
    }

    [Fact]
    public void RemoveDiscount_ShouldSetDiscountFeeToNull()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();

        // Act
        var result = invoiceItem.RemoveDiscount();

        // Assert
        result.Should().BeSameAs(invoiceItem); // Should return same instance
        invoiceItem.DiscountFee.Should().BeNull();
    }

    [Fact]
    public void RemoveAdditionalFee_ShouldSetAdditionalFeeToNull()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();

        // Act
        var result = invoiceItem.RemoveAdditionalFee();

        // Assert
        result.Should().BeSameAs(invoiceItem); // Should return same instance
        invoiceItem.AdditionalFee.Should().BeNull();
    }

    [Fact]
    public void Create_FluentAPI_ShouldAllowMethodChaining()
    {
        // Arrange
        var discountFee = DiscountFee.Create(5.0m, FeeStandardUnit.Percent);
        var additionalFee = AdditionalFee.Create(100.0m, FeeStandardUnit.NGN);

        // Act
        var invoiceItem = InvoiceItem.Create(_businessItemId, _invoiceId, _validQuantity, _validUnitPrice, null, null)
            .WithDiscount(discountFee)
            .WithAdditionalFee(additionalFee);

        // Assert
        invoiceItem.DiscountFee.Should().Be(discountFee);
        invoiceItem.AdditionalFee.Should().Be(additionalFee);
    }

    [Fact]
    public void RemovalMethods_FluentAPI_ShouldAllowMethodChaining()
    {
        // Arrange
        var invoiceItem = CreateTestInvoiceItem();

        // Act
        var result = invoiceItem.RemoveDiscount().RemoveAdditionalFee();

        // Assert
        result.Should().BeSameAs(invoiceItem);
        invoiceItem.DiscountFee.Should().BeNull();
        invoiceItem.AdditionalFee.Should().BeNull();
    }

    [Theory]
    [InlineData(1, FeeStandardUnit.NGN)]
    [InlineData(50.5, FeeStandardUnit.NGN)]
    [InlineData(10.0, FeeStandardUnit.Percent)]
    [InlineData(99.99, FeeStandardUnit.Percent)]
    public void Create_WithDifferentFeeTypes_ShouldWork(decimal amount, FeeStandardUnit unit)
    {
        // Arrange
        var discountFee = DiscountFee.Create(amount, unit);
        var additionalFee = AdditionalFee.Create(amount, unit);

        // Act
        var invoiceItem = InvoiceItem.Create(
            _businessItemId,
            _invoiceId,
            _validQuantity,
            _validUnitPrice,
            discountFee,
            additionalFee);

        // Assert
        invoiceItem.DiscountFee.Should().Be(discountFee);
        invoiceItem.AdditionalFee.Should().Be(additionalFee);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(999)]
    public void Create_WithDifferentQuantities_ShouldWork(int quantity)
    {
        // Act
        var invoiceItem = InvoiceItem.Create(_businessItemId, _invoiceId, quantity, _validUnitPrice, null, null);

        // Assert
        invoiceItem.Quantity.Should().Be(quantity);
    }

    private InvoiceItem CreateTestInvoiceItem()
    {
        return InvoiceItem.Create(
            _businessItemId,
            _invoiceId,
            _validQuantity,
            _validUnitPrice,
            _validDiscountFee,
            _validAdditionalFee);
    }
}