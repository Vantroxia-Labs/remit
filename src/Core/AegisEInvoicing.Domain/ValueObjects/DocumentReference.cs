namespace AegisEInvoicing.Domain.ValueObjects;

public class DocumentReference : ValueObject
{
    public string IRN { get; } = null!;
    public DateOnly IssueDate { get; }

    private DocumentReference()
    {
        IssueDate = default;
    }

    private DocumentReference(string irn, DateOnly issueDate)
    {
        IRN = irn;
        IssueDate = issueDate;
    }

    public static DocumentReference Create(string irn, DateOnly issueDate)
    {
        if (string.IsNullOrWhiteSpace(irn))
            throw new ArgumentException("IRN cannot be null or empty", nameof(irn));

        return new DocumentReference(irn, issueDate);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IRN;
        yield return IssueDate;
    }
}