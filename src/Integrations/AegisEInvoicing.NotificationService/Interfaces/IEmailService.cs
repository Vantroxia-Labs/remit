using AegisEInvoicing.NotificationService.Models;

namespace AegisEInvoicing.NotificationService.Interfaces;

public interface IEmailService
{
    Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default);
    Task<EmailResult> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default);
}
