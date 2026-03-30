using AegisEInvoicing.Domain.Enums;

namespace AegisEInvoicing.Domain.ValueObjects;

public class AdditionalFee : ValueObject
{
    public decimal Amount { get; }
    public FeeStandardUnit Code { get; }

    private AdditionalFee() { }

    private AdditionalFee(decimal amount, FeeStandardUnit code)
    {
        Amount = amount;
        Code = code;
    }

    public static AdditionalFee Create(decimal amount, FeeStandardUnit code)
    {
        if (code == FeeStandardUnit.Percent)
        {
            if (amount < 0 || amount > 100)
                throw new BadRequestException("Additional Fee Percentage cannot be less than 0% or greater than 100%");

            return new AdditionalFee(amount, code);
        }

        if (amount < 0)
            throw new BadRequestException("Additional Fee Amount cannot be less than 0");

        return new AdditionalFee(
            amount,
            code);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Code;
    }
}