using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;

namespace AegisEInvoicing.NotificationService.Services;

public class AwsSesEmailService : IEmailService, IDisposable
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly AwsSesConfiguration _configuration;
    private readonly ILogger<AwsSesEmailService> _logger;
    private bool _disposed = false;

    public AwsSesEmailService(
        IAmazonSimpleEmailService sesClient,
        IOptions<AwsSesConfiguration> configuration,
        ILogger<AwsSesEmailService> logger)
    {
        _sesClient = sesClient ?? throw new ArgumentNullException(nameof(sesClient));
        _configuration = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateEmailMessage(message);

            var request = BuildSendEmailRequest(message);

            _logger.LogInformation("Sending email to {Recipients} with subject: {Subject}",
                string.Join(", ", GetAllRecipients(message)), message.Subject);

            var response = await ExecuteWithRetryAsync(
                () => _sesClient.SendEmailAsync(request, cancellationToken),
                _configuration.MaxRetries,
                cancellationToken);

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}", response.MessageId);

            return EmailResult.Success(response.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipients}",
                string.Join(", ", GetAllRecipients(message)));

            return EmailResult.Failure($"Failed to send email: {ex.Message}", ex);
        }
    }

    public async Task<EmailResult> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();
        var errors = new List<string>();

        foreach (var message in messages)
        {
            var result = await SendEmailAsync(message, cancellationToken);

            if (result.IsSuccess)
            {
                results.Add(result.MessageId!);
            }
            else
            {
                errors.Add(result.ErrorMessage!);
            }
        }

        if (errors.Any())
        {
            _logger.LogWarning("Bulk email sending completed with {ErrorCount} errors out of {TotalCount} messages",
                errors.Count, messages.Count);

            return EmailResult.Failure($"Bulk send completed with errors: {string.Join("; ", errors)}");
        }

        _logger.LogInformation("Bulk email sending completed successfully for {Count} messages", messages.Count);
        return EmailResult.Success(string.Join(",", results));
    }

    private SendEmailRequest BuildSendEmailRequest(EmailMessage message)
    {
        var destination = new Destination();

        if (!string.IsNullOrEmpty(message.To))
            destination.ToAddresses.Add(message.To);

        destination.ToAddresses.AddRange(message.ToAddresses);
        destination.CcAddresses.AddRange(message.CcAddresses);
        destination.BccAddresses.AddRange(message.BccAddresses);

        var body = new Body();

        if (!string.IsNullOrEmpty(message.HtmlBody))
            body.Html = new Content(message.HtmlBody);

        if (!string.IsNullOrEmpty(message.TextBody))
            body.Text = new Content(message.TextBody);

        var request = new SendEmailRequest
        {
            Source = BuildSourceAddress(message),
            Destination = destination,
            Message = new Message
            {
                Subject = new Content(message.Subject),
                Body = body
            }
        };

        if (!string.IsNullOrEmpty(message.ReplyToEmail))
            request.ReplyToAddresses.Add(message.ReplyToEmail);

        // Add tags for tracking
        foreach (var tag in message.Tags)
        {
            request.Tags.Add(new MessageTag { Name = tag.Key, Value = tag.Value });
        }

        return request;
    }

    private string BuildSourceAddress(EmailMessage message)
    {
        var fromEmail = message.FromEmail ?? _configuration.DefaultFromEmail;
        var fromName = message.FromName ?? _configuration.DefaultFromName;

        if (string.IsNullOrEmpty(fromName))
            return fromEmail;

        return $"{fromName} <{fromEmail}>";
    }

    private async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxRetries && IsRetriableException(ex))
            {
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                _logger.LogWarning("Attempt {Attempt} failed, retrying in {Delay}ms: {Error}",
                    attempt + 1, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay, cancellationToken);
            }
        }

        throw new InvalidOperationException("This should never be reached");
    }

    private static bool IsRetriableException(Exception ex)
    {
        return ex is AmazonSimpleEmailServiceException sesEx &&
               (sesEx.StatusCode == HttpStatusCode.InternalServerError ||
                sesEx.StatusCode == HttpStatusCode.BadGateway ||
                sesEx.StatusCode == HttpStatusCode.ServiceUnavailable ||
                sesEx.StatusCode == HttpStatusCode.GatewayTimeout ||
                sesEx.ErrorCode == "Throttling");
    }

    private static void ValidateEmailMessage(EmailMessage message)
    {
        if (string.IsNullOrEmpty(message.Subject))
            throw new ArgumentException("Email subject is required");

        if (string.IsNullOrEmpty(message.HtmlBody) && string.IsNullOrEmpty(message.TextBody))
            throw new ArgumentException("Email body (HTML or text) is required");

        var recipients = GetAllRecipients(message);
        if (!recipients.Any())
            throw new ArgumentException("At least one recipient is required");
    }

    private static List<string> GetAllRecipients(EmailMessage message)
    {
        var recipients = new List<string>();

        if (!string.IsNullOrEmpty(message.To))
            recipients.Add(message.To);

        recipients.AddRange(message.ToAddresses);
        recipients.AddRange(message.CcAddresses);
        recipients.AddRange(message.BccAddresses);

        return recipients.Where(email => !string.IsNullOrEmpty(email)).ToList();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _sesClient?.Dispose();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }
}