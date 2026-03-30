using AegisEInvoicing.Domain.Common.Implementation;
using AegisEInvoicing.Domain.Entities.BusinessManagement;

namespace AegisEInvoicing.Domain.Entities.InvoiceManagement;

public class InvoiceItem : AuditableEntity
{
    public Guid BusinessItemId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public decimal Quantity { get; private set; }
    public DiscountFee? DiscountFee { get; private set; }
    public AdditionalFee? AdditionalFee { get; private set; }

    /// <summary>
    /// Snapshot of the unit price at the time the invoice was created.
    /// This ensures price changes don't affect previously created invoices.
    /// </summary>
    public decimal UnitPriceSnapshot { get; private set; }

    // Navigation properties
    public Invoice Invoice { get; private set; } = null!;
    public BusinessItem BusinessItem { get; private set; } = null!;

    // EF Constructor
    private InvoiceItem() { }

    // Factory Methods

    public static InvoiceItem Create(
        Guid businessItemId,
        Guid invoiceId,
        decimal quantity,
        decimal unitPriceSnapshot,
        DiscountFee? discountFee,
        AdditionalFee? additionalFee)
    {
        ValidateParameters(businessItemId, invoiceId, quantity, unitPriceSnapshot);

        return new InvoiceItem
        {
            BusinessItemId = businessItemId,
            InvoiceId = invoiceId,
            Quantity = quantity,
            UnitPriceSnapshot = unitPriceSnapshot,
            DiscountFee = discountFee,
            AdditionalFee = additionalFee
        };
    }

    private static void ValidateParameters(Guid businessItemId, Guid invoiceId, decimal quantity, decimal unitPriceSnapshot)
    {
        if (businessItemId == Guid.Empty)
            throw new ArgumentException("BusinessItemId cannot be empty", nameof(businessItemId));

        if (invoiceId == Guid.Empty)
            throw new ArgumentException("InvoiceId cannot be empty", nameof(invoiceId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero", nameof(quantity));

        if (unitPriceSnapshot < 0)
            throw new ArgumentException("Unit price snapshot cannot be negative", nameof(unitPriceSnapshot));
    }

    // Optional: Methods to update optional properties after creation
    public InvoiceItem WithDiscount(DiscountFee discountFee)
    {
        ArgumentNullException.ThrowIfNull(discountFee);
        DiscountFee = discountFee;
        return this;
    }

    public InvoiceItem WithAdditionalFee(AdditionalFee additionalFee)
    {
        ArgumentNullException.ThrowIfNull(additionalFee);
        AdditionalFee = additionalFee;
        return this;
    }

    public void UpdateQuantity(decimal quantity)
    {
        if(quantity <= 0)
            throw new BadRequestException("Quantity cannot be less than 0", nameof(quantity));

        Quantity = quantity;
    }

    public void UpdateDiscountFee(DiscountFee? discountFee)
    {
        DiscountFee = discountFee;
    }

    public void UpdateAdditionalFee(AdditionalFee? additionalFee)
    {
        AdditionalFee = additionalFee;
    }

    public InvoiceItem RemoveDiscount()
    {
        DiscountFee = null;
        return this;
    }

    public InvoiceItem RemoveAdditionalFee()
    {
        AdditionalFee = null;
        return this;
    }
}