namespace AegisEInvoicing.NotificationService.Models;

public enum EmailErrorType
{
    Unknown,
    Authentication,
    Authorization,
    NetworkTimeout,
    ServerError,
    RateLimited,
    InvalidRecipient,
    MessageTooLarge,
    QuotaExceeded,
    Configuration
}
