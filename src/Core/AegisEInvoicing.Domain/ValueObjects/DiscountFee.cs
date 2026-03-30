using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.ValueObjects;

public class DiscountFee : ValueObject
{
    public decimal Amount { get; }
    public FeeStandardUnit Code { get; }

    private DiscountFee() { }

    private DiscountFee(decimal amount, FeeStandardUnit code)
    {
        Amount = amount;
        Code = code;
    }

    public static DiscountFee Create(decimal amount, FeeStandardUnit code)
    {
        if (code == FeeStandardUnit.Percent)
        {
            if (amount < 0 || amount > 100)
                throw new BadRequestException("Discount Fee Percentage cannot be less than 0% or greater than 100%");

            return new DiscountFee(amount, code);
        }

        if (amount < 0)
            throw new BadRequestException("Discount Fee Amount cannot be less than 0");

        return new DiscountFee(
            amount,
            code);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Code;
    }
}