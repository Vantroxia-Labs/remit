namespace AegisEInvoicing.Domain.ValueObjects;

public class DeliveryPeriod : ValueObject
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    private DeliveryPeriod()
    {
        StartDate = default;
    }

    private DeliveryPeriod(DateOnly startDate, DateOnly endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
    }

    public static DeliveryPeriod Create(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new ArgumentException("End date cannot be before start date");

        return new DeliveryPeriod(startDate, endDate);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return StartDate;
        yield return EndDate;
    }
}