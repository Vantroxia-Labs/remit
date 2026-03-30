using Azure;
using Azure.Communication.Email;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

using AzureEmailMessage = Azure.Communication.Email.EmailMessage;
using AzureEmailAttachment = Azure.Communication.Email.EmailAttachment;
using AzureEmailAddress = Azure.Communication.Email.EmailAddress;
using AzureEmailContent = Azure.Communication.Email.EmailContent;
using AzureEmailRecipients = Azure.Communication.Email.EmailRecipients;
using LocalEmailMessage = AegisEInvoicing.NotificationService.Models.EmailMessage;

namespace AegisEInvoicing.NotificationService.Services;

public class AzureCommunicationEmailService : IEmailService
{
    private readonly EmailClient _emailClient;
    private readonly AzureCommunicationConfiguration _config;
    private readonly ILogger<AzureCommunicationEmailService> _logger;
    private readonly SemaphoreSlim _bulkOperationSemaphore;

    public AzureCommunicationEmailService(
        IOptions<AzureCommunicationConfiguration> config,
        ILogger<AzureCommunicationEmailService> logger)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_config.ConnectionString))
            throw new InvalidOperationException("Azure Communication Service connection string is not configured.");

        _emailClient = new EmailClient(_config.ConnectionString);
        _bulkOperationSemaphore = new SemaphoreSlim(_config.MaxConcurrentOperations);
    }

    public async Task<EmailResult> SendEmailAsync(LocalEmailMessage message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Add BCC email from configuration if available
            if (!string.IsNullOrWhiteSpace(_config.DefaultBccEmail))
            {
                message.BccAddresses.Add(_config.DefaultBccEmail);
            }
            
            ValidateEmailMessage(message);

            _logger.LogInformation("Sending email via Azure Communication Service to {Recipients} with subject: {Subject}",
                string.Join(", ", GetAllRecipients(message)), message.Subject);

            // Build Azure email content
            var emailContent = new AzureEmailContent(message.Subject)
            {
                Html = message.HtmlBody,
                PlainText = message.TextBody
            };

            // Build Azure email message
            var azureEmailMessage = new AzureEmailMessage(
                senderAddress: _config.DefaultFromEmail,
                content: emailContent,
                recipients: BuildAzureRecipients(message));

            // Add attachments if any
            if (message.Attachments.Any())
            {
                foreach (var attachment in message.Attachments)
                {
                    var azureAttachment = new AzureEmailAttachment(
                        name: attachment.FileName,
                        contentType: attachment.ContentType,
                        content: new BinaryData(attachment.Content));

                    azureEmailMessage.Attachments.Add(azureAttachment);
                }
            }

            // Send with retry logic
            var messageId = await ExecuteWithRetryAsync(async () =>
            {
                EmailSendOperation emailSendOperation = await _emailClient.SendAsync(
                    WaitUntil.Started,
                    azureEmailMessage,
                    cancellationToken);

                return emailSendOperation.Id;
            }, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("Email sent successfully via Azure Communication Service. MessageId: {MessageId}, Duration: {Duration}ms",
                messageId, stopwatch.ElapsedMilliseconds);

            return EmailResult.Success(messageId, new Dictionary<string, object>
            {
                ["Duration"] = stopwatch.ElapsedMilliseconds,
                ["Recipients"] = GetAllRecipients(message).Count,
                ["Provider"] = "AzureCommunicationService"
            });
        }
        catch (RequestFailedException ex)
        {
            stopwatch.Stop();
            var errorType = CategorizeAzureException(ex);

            _logger.LogError(ex, "Azure Communication Service failed to send email. ErrorCode: {ErrorCode}, Status: {Status}, Duration: {Duration}ms",
                ex.ErrorCode, ex.Status, stopwatch.ElapsedMilliseconds);

            return EmailResult.Failure($"Failed to send email via Azure Communication Service: {ex.Message}", ex, errorType);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            _logger.LogError(ex, "Unexpected error sending email via Azure Communication Service after {Duration}ms",
                stopwatch.ElapsedMilliseconds);

            return EmailResult.Failure($"Failed to send email: {ex.Message}", ex, EmailErrorType.Unknown);
        }
    }

    public async Task<EmailResult> SendBulkEmailAsync(List<LocalEmailMessage> messages, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var results = new ConcurrentBag<(bool Success, string? MessageId, string? Error)>();
        var semaphore = new SemaphoreSlim(_config.MaxConcurrentOperations);

        try
        {
            var tasks = messages.Select(async message =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var result = await SendEmailAsync(message, cancellationToken);
                    results.Add((result.IsSuccess, result.MessageId, result.ErrorMessage));
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var successful = results.Count(r => r.Success);
            var failed = results.Count(r => !r.Success);

            stopwatch.Stop();

            _logger.LogInformation("Bulk email operation completed via Azure Communication Service. Success: {Success}, Failed: {Failed}, Duration: {Duration}ms",
                successful, failed, stopwatch.ElapsedMilliseconds);

            if (failed == 0)
            {
                return EmailResult.Success($"All {successful} emails sent successfully", new Dictionary<string, object>
                {
                    ["TotalSent"] = successful,
                    ["TotalFailed"] = failed,
                    ["Duration"] = stopwatch.ElapsedMilliseconds,
                    ["Provider"] = "AzureCommunicationService"
                });
            }
            else if (successful == 0)
            {
                var errors = string.Join("; ", results.Where(r => !r.Success).Select(r => r.Error));
                return EmailResult.Failure($"All {failed} emails failed: {errors}", errorType: EmailErrorType.Unknown);
            }
            else
            {
                return EmailResult.Success($"Partial success: {successful} sent, {failed} failed", new Dictionary<string, object>
                {
                    ["TotalSent"] = successful,
                    ["TotalFailed"] = failed,
                    ["Duration"] = stopwatch.ElapsedMilliseconds,
                    ["Provider"] = "AzureCommunicationService",
                    ["IsPartialSuccess"] = true
                });
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Bulk email operation failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            return EmailResult.Failure($"Bulk email operation failed: {ex.Message}", ex, EmailErrorType.Unknown);
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    private static AzureEmailRecipients BuildAzureRecipients(LocalEmailMessage message)
    {
        var recipients = new AzureEmailRecipients();

        // Add To recipients
        var toAddresses = message.ToAddresses.Any() ? message.ToAddresses : new List<string> { message.To };
        foreach (var email in toAddresses.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            recipients.To.Add(new AzureEmailAddress(email));
        }

        // Add CC recipients
        foreach (var email in message.CcAddresses.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            recipients.CC.Add(new AzureEmailAddress(email));
        }

        // Add BCC recipients
        foreach (var email in message.BccAddresses.Where(e => !string.IsNullOrWhiteSpace(e)))
        {
            recipients.BCC.Add(new AzureEmailAddress(email));
        }

        return recipients;
    }

    private async Task<string> ExecuteWithRetryAsync(Func<Task<string>> action, CancellationToken cancellationToken)
    {
        var delay = _config.InitialRetryDelay;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= _config.MaxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (RequestFailedException ex) when (IsRetriableError(CategorizeAzureException(ex)) && attempt < _config.MaxRetries)
            {
                lastException = ex;
                _logger.LogWarning(ex, "Attempt {Attempt} failed, retrying in {Delay}ms. ErrorCode: {ErrorCode}",
                    attempt + 1, delay.TotalMilliseconds, ex.ErrorCode);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, _config.MaxRetryDelay.TotalMilliseconds));
            }
        }

        throw lastException ?? new InvalidOperationException("Retry logic failed without capturing an exception.");
    }

    private static EmailErrorType CategorizeAzureException(RequestFailedException ex)
    {
        return ex.Status switch
        {
            401 => EmailErrorType.Authentication,
            403 => EmailErrorType.Authorization,
            429 => EmailErrorType.RateLimited,
            408 => EmailErrorType.NetworkTimeout,
            413 => EmailErrorType.MessageTooLarge,
            >= 500 => EmailErrorType.ServerError,
            _ => EmailErrorType.Unknown
        };
    }

    private static bool IsRetriableError(EmailErrorType errorType)
    {
        return errorType switch
        {
            EmailErrorType.NetworkTimeout => true,
            EmailErrorType.ServerError => true,
            EmailErrorType.RateLimited => true,
            EmailErrorType.Unknown => true,
            _ => false
        };
    }

    private void ValidateEmailMessage(LocalEmailMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.To) && !message.ToAddresses.Any())
            throw new ArgumentException("At least one recipient is required", nameof(message));

        if (string.IsNullOrWhiteSpace(message.Subject))
            throw new ArgumentException("Subject is required", nameof(message));

        if (string.IsNullOrWhiteSpace(message.HtmlBody) && string.IsNullOrWhiteSpace(message.TextBody))
            throw new ArgumentException("Email body (HTML or Text) is required", nameof(message));
    }

    private static List<string> GetAllRecipients(LocalEmailMessage message)
    {
        var recipients = new List<string>();

        if (!string.IsNullOrWhiteSpace(message.To))
            recipients.Add(message.To);

        recipients.AddRange(message.ToAddresses);
        recipients.AddRange(message.CcAddresses);
        recipients.AddRange(message.BccAddresses);

        return recipients.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();
    }
}