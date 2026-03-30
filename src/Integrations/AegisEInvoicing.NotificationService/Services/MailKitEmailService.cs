using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace AegisEInvoicing.NotificationService.Services;

public class MailKitEmailService : IEmailService, IDisposable
{
    private readonly ISmtpConnectionManager _connectionManager;
    private readonly MailKitConfiguration _config;
    private readonly ILogger<MailKitEmailService> _logger;
    private readonly SemaphoreSlim _bulkOperationSemaphore;
    private bool _disposed;

    public MailKitEmailService(
        ISmtpConnectionManager connectionManager,
        IOptions<MailKitConfiguration> config,
        ILogger<MailKitEmailService> logger)
    {
        _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _bulkOperationSemaphore = new SemaphoreSlim(_config.MaxConcurrentOperations);
    }

    public async Task<EmailResult> SendEmailAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            ValidateEmailMessage(message);
            var mimeMessage = BuildMimeMessage(message);

            _logger.LogInformation("Sending email to {Recipients} with subject: {Subject}",
                string.Join(", ", GetAllRecipients(message)), message.Subject);

            var messageId = await ExecuteWithRetryAsync(async () =>
            {
                var connection = await _connectionManager.GetConnectionAsync(cancellationToken);
                try
                {
                    return await connection.SendAsync(mimeMessage, cancellationToken);
                }
                finally
                {
                    await _connectionManager.ReturnConnectionAsync(connection, true);
                }
            }, cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation("Email sent successfully. MessageId: {MessageId}, Duration: {Duration}ms",
                messageId, stopwatch.ElapsedMilliseconds);

            return EmailResult.Success(messageId, new Dictionary<string, object>
            {
                ["Duration"] = stopwatch.ElapsedMilliseconds,
                ["Recipients"] = GetAllRecipients(message).Count
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var errorType = CategorizeException(ex);
            _logger.LogError(ex, "Failed to send email to {Recipients} after {Duration}ms. ErrorType: {ErrorType}",
                string.Join(", ", GetAllRecipients(message)), stopwatch.ElapsedMilliseconds, errorType);

            return EmailResult.Failure($"Failed to send email: {ex.Message}", ex, errorType);
        }
    }

    public async Task<EmailResult> SendBulkEmailAsync(List<EmailMessage> messages, CancellationToken cancellationToken = default)
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

            stopwatch.Stop();

            var successful = results.Count(r => r.Success);
            var failed = results.Count(r => !r.Success);
            var messageIds = results.Where(r => r.Success).Select(r => r.MessageId!).ToList();
            var errors = results.Where(r => !r.Success).Select(r => r.Error!).ToList();

            _logger.LogInformation("Bulk email completed: {Successful} successful, {Failed} failed in {Duration}ms",
                successful, failed, stopwatch.ElapsedMilliseconds);

            if (failed > 0)
            {
                return EmailResult.Failure($"Bulk send completed with {failed} failures: {string.Join("; ", errors.Take(3))}");
            }

            return EmailResult.Success(string.Join(",", messageIds), new Dictionary<string, object>
            {
                ["Duration"] = stopwatch.ElapsedMilliseconds,
                ["TotalMessages"] = messages.Count,
                ["SuccessfulMessages"] = successful
            });
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Bulk email operation failed after {Duration}ms", stopwatch.ElapsedMilliseconds);
            return EmailResult.Failure($"Bulk email operation failed: {ex.Message}", ex);
        }
        finally
        {
            semaphore?.Dispose();
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        var delay = _config.InitialRetryDelay;

        for (int attempt = 0; attempt <= _config.MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < _config.MaxRetries)
            {
                lastException = ex;
                var errorType = CategorizeException(ex);

                if (!IsRetriableError(errorType))
                {
                    throw;
                }

                _logger.LogWarning("Attempt {Attempt} failed, retrying in {Delay}ms: {Error}",
                    attempt + 1, delay.TotalMilliseconds, ex.Message);

                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, _config.MaxRetryDelay.TotalMilliseconds));
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed after all retries");
    }

    private MimeMessage BuildMimeMessage(EmailMessage message)
    {
        var mimeMessage = new MimeMessage();

        // From
        var fromEmail = message.FromEmail ?? _config.DefaultFromEmail;
        var fromName = message.FromName ?? _config.DefaultFromName;
        mimeMessage.From.Add(new MailboxAddress(fromName, fromEmail));

        // Recipients
        if (!string.IsNullOrEmpty(message.To))
            mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        foreach (var to in message.ToAddresses)
            mimeMessage.To.Add(MailboxAddress.Parse(to));

        foreach (var cc in message.CcAddresses)
            mimeMessage.Cc.Add(MailboxAddress.Parse(cc));

        foreach (var bcc in message.BccAddresses)
            mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));

        // Reply-To
        if (!string.IsNullOrEmpty(message.ReplyToEmail))
            mimeMessage.ReplyTo.Add(MailboxAddress.Parse(message.ReplyToEmail));

        // Subject
        mimeMessage.Subject = message.Subject;

        // Body
        var bodyBuilder = new BodyBuilder();

        if (!string.IsNullOrEmpty(message.TextBody))
            bodyBuilder.TextBody = message.TextBody;

        if (!string.IsNullOrEmpty(message.HtmlBody))
            bodyBuilder.HtmlBody = message.HtmlBody;

        // TODO: Add attachment support if needed
        // foreach (var attachment in message.Attachments)
        //     bodyBuilder.Attachments.Add(attachment);

        mimeMessage.Body = bodyBuilder.ToMessageBody();

        // Headers for tracking
        foreach (var tag in message.Tags)
        {
            mimeMessage.Headers.Add($"X-{tag.Key}", tag.Value);
        }

        return mimeMessage;
    }

    private static EmailErrorType CategorizeException(Exception ex)
    {
        return ex switch
        {
            AuthenticationException => EmailErrorType.Authentication,
            SmtpCommandException smtp => smtp.ErrorCode switch
            {
                SmtpErrorCode.MessageNotAccepted => EmailErrorType.RateLimited,
                SmtpErrorCode.RecipientNotAccepted => EmailErrorType.InvalidRecipient,
                _ when smtp.StatusCode >= SmtpStatusCode.ErrorInProcessing => EmailErrorType.ServerError,
                _ => EmailErrorType.Unknown
            },
            TimeoutException => EmailErrorType.NetworkTimeout,
            SocketException => EmailErrorType.NetworkTimeout,
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
            _connectionManager?.Dispose();
            _bulkOperationSemaphore?.Dispose();
            _disposed = true;
        }
    }
}
