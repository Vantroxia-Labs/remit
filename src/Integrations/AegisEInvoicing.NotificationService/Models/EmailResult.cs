namespace AegisEInvoicing.NotificationService.Models;

public class EmailResult
{
    public bool IsSuccess { get; private set; }
    public string? MessageId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public Exception? Exception { get; private set; }
    public EmailErrorType ErrorType { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private EmailResult()
    {
        Metadata = new Dictionary<string, object>();
    }

    public static EmailResult Success(string messageId, Dictionary<string, object>? metadata = null)
    {
        var result = new EmailResult
        {
            IsSuccess = true,
            MessageId = messageId
        };

        if (metadata != null)
        {
            foreach (var kvp in metadata)
                result.Metadata[kvp.Key] = kvp.Value;
        }

        return result;
    }

    public static EmailResult Failure(string errorMessage, Exception? exception = null,
        EmailErrorType errorType = EmailErrorType.Unknown)
    {
        return new EmailResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            Exception = exception,
            ErrorType = errorType
        };
    }
}