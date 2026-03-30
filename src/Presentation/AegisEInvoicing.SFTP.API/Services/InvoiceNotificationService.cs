using AegisEInvoicing.SFTP.API.Configuration;
using AegisEInvoicing.SFTP.API.Models;
using AegisEInvoicing.SFTP.API.Services.Interfaces;
using AegisEInvoicing.NotificationService.Interfaces;
using AegisEInvoicing.NotificationService.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;

namespace AegisEInvoicing.SFTP.API.Services;

/// <summary>
/// Service for sending invoice-related email notifications
/// </summary>
public class InvoiceNotificationService : IInvoiceNotificationService
{
    private readonly IEmailService _emailService;
    private readonly NotificationConfiguration _notificationConfig;
    private readonly ILogger<InvoiceNotificationService> _logger;

    public InvoiceNotificationService(
        IEmailService emailService,
        IOptions<NotificationConfiguration> notificationConfig,
        ILogger<InvoiceNotificationService> logger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _notificationConfig = notificationConfig.Value ?? throw new ArgumentNullException(nameof(notificationConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendInvoiceSuccessNotificationAsync(
        Guid invoiceId,
        Guid partyId,
        string irn,
        string fileName,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_notificationConfig.EnableEmailNotifications || 
                !_notificationConfig.SendNotificationForEachInvoice ||
                !_notificationConfig.SuccessNotificationRecipients.Any())
            {
                _logger.LogDebug("Success email notifications are disabled or no recipients configured");
                return true; // Return true as this is not an error condition
            }

            var subject = _notificationConfig.SuccessEmailSubject
                .Replace("{InvoiceId}", invoiceId.ToString())
                .Replace("{FileName}", fileName)
                .Replace("{IRN}", irn);

            var htmlBody = GenerateSuccessEmailBody(invoiceId, partyId, irn, fileName, connectionId);

            var emailMessage = new EmailMessage
            {
                ToAddresses = _notificationConfig.SuccessNotificationRecipients,
                Subject = subject,
                HtmlBody = htmlBody,
                Tags = new Dictionary<string, string>
                {
                    ["Type"] = "InvoiceSuccess",
                    ["InvoiceId"] = invoiceId.ToString(),
                    ["ConnectionId"] = connectionId,
                    ["FileName"] = fileName
                }
            };

            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Success notification sent for invoice {InvoiceId}, MessageId: {MessageId}",
                    invoiceId, result.MessageId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send success notification for invoice {InvoiceId}: {ErrorMessage}",
                    invoiceId, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending success notification for invoice {InvoiceId}", invoiceId);
            return false;
        }
    }

    public async Task<bool> SendInvoiceErrorNotificationAsync(
        string fileName,
        string connectionId,
        string errorMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_notificationConfig.EnableEmailNotifications || 
                !_notificationConfig.ErrorNotificationRecipients.Any())
            {
                _logger.LogDebug("Error email notifications are disabled or no recipients configured");
                return true; // Return true as this is not an error condition
            }

            var subject = _notificationConfig.ErrorEmailSubject
                .Replace("{FileName}", fileName)
                .Replace("{ConnectionId}", connectionId);

            var htmlBody = GenerateErrorEmailBody(fileName, connectionId, errorMessage);

            var emailMessage = new EmailMessage
            {
                ToAddresses = _notificationConfig.ErrorNotificationRecipients,
                Subject = subject,
                HtmlBody = htmlBody,
                Tags = new Dictionary<string, string>
                {
                    ["Type"] = "InvoiceError",
                    ["ConnectionId"] = connectionId,
                    ["FileName"] = fileName
                }
            };

            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Error notification sent for file {FileName}, MessageId: {MessageId}",
                    fileName, result.MessageId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send error notification for file {FileName}: {ErrorMessage}",
                    fileName, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending error notification for file {FileName}", fileName);
            return false;
        }
    }

    public async Task<bool> SendProcessingSummaryNotificationAsync(
        ProcessingStatistics statistics,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_notificationConfig.EnableEmailNotifications || 
                !_notificationConfig.SendSummaryNotifications ||
                !_notificationConfig.SummaryNotificationRecipients.Any())
            {
                _logger.LogDebug("Summary email notifications are disabled or no recipients configured");
                return true; // Return true as this is not an error condition
            }

            var subject = _notificationConfig.SummaryEmailSubject
                .Replace("{Date}", statistics.ProcessingStartTime.ToString("yyyy-MM-dd"))
                .Replace("{TotalFiles}", statistics.TotalFilesProcessed.ToString());

            var htmlBody = GenerateSummaryEmailBody(statistics);

            var emailMessage = new EmailMessage
            {
                ToAddresses = _notificationConfig.SummaryNotificationRecipients,
                Subject = subject,
                HtmlBody = htmlBody,
                Tags = new Dictionary<string, string>
                {
                    ["Type"] = "ProcessingSummary",
                    ["ProcessingDate"] = statistics.ProcessingStartTime.ToString("yyyy-MM-dd"),
                    ["TotalFiles"] = statistics.TotalFilesProcessed.ToString(),
                    ["SuccessfulFiles"] = statistics.SuccessfulFiles.ToString(),
                    ["ErrorFiles"] = statistics.ErrorFiles.ToString()
                }
            };

            var result = await _emailService.SendEmailAsync(emailMessage, cancellationToken);

            if (result.IsSuccess)
            {
                _logger.LogInformation("Summary notification sent, MessageId: {MessageId}", result.MessageId);
                return true;
            }
            else
            {
                _logger.LogError("Failed to send summary notification: {ErrorMessage}", result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending summary notification");
            return false;
        }
    }

    #region Private Methods

    private string GenerateSuccessEmailBody(Guid invoiceId, Guid partyId, string irn, string fileName, string connectionId)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"utf-8\">");
        sb.AppendLine("    <title>Invoice Successfully Processed</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; color: #333; }");
        sb.AppendLine("        .container { max-width: 600px; margin: 0 auto; }");
        sb.AppendLine("        .header { background-color: #28a745; color: white; padding: 15px; text-align: center; }");
        sb.AppendLine("        .content { background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; }");
        sb.AppendLine("        .info-table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
        sb.AppendLine("        .info-table td { padding: 8px 12px; border-bottom: 1px solid #dee2e6; }");
        sb.AppendLine("        .info-table .label { font-weight: bold; background-color: #e9ecef; width: 30%; }");
        sb.AppendLine("        .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <div class=\"header\">");
        sb.AppendLine("            <h2>✅ Invoice Successfully Processed</h2>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"content\">");
        sb.AppendLine("            <p>An invoice has been successfully processed through the EInvoice Integrator system.</p>");
        sb.AppendLine("            <table class=\"info-table\">");
        sb.AppendLine($"                <tr><td class=\"label\">Invoice ID:</td><td>{invoiceId}</td></tr>");
        sb.AppendLine($"                <tr><td class=\"label\">Party ID:</td><td>{partyId}</td></tr>");
        sb.AppendLine($"                <tr><td class=\"label\">IRN:</td><td>{irn}</td></tr>");
        sb.AppendLine($"                <tr><td class=\"label\">File Name:</td><td>{fileName}</td></tr>");
        sb.AppendLine($"                <tr><td class=\"label\">Connection ID:</td><td>{connectionId}</td></tr>");
        sb.AppendLine($"                <tr><td class=\"label\">Processed At:</td><td>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td></tr>");
        sb.AppendLine("            </table>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"footer\">");
        sb.AppendLine("            <p>This is an automated notification from EInvoice Integrator Background Service.</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private string GenerateErrorEmailBody(string fileName, string connectionId, string errorMessage)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"utf-8\">");
        sb.AppendLine("    <title>Invoice Processing Failed</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; color: #333; }");
        sb.AppendLine("        .container { max-width: 600px; margin: 0 auto; }");
        sb.AppendLine("        .header { background-color: #dc3545; color: white; padding: 15px; text-align: center; }");
        sb.AppendLine("        .content { background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; }");
        sb.AppendLine("        .info-table { width: 100%; border-collapse: collapse; margin: 15px 0; }");
        sb.AppendLine("        .info-table td { padding: 8px 12px; border-bottom: 1px solid #dee2e6; }");
        sb.AppendLine("        .info-table .label { font-weight: bold; background-color: #e9ecef; width: 30%; }");
        sb.AppendLine("        .error-message { background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 10px; margin: 15px 0; border-radius: 4px; }");
        sb.AppendLine("        .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <div class=\"header\">");
        sb.AppendLine("            <h2>❌ Invoice Processing Failed</h2>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"content\">");
        sb.AppendLine("            <p>An error occurred while processing an invoice file through the EInvoice Integrator system.</p>");
        sb.AppendLine("            <table class=\"info-table\">");
        sb.AppendLine($"                <tr><td class=\"label\">File Name:</td><td>{fileName}</td></tr>");
        sb.AppendLine($"                <tr><td class=\"label\">Connection ID:</td><td>{connectionId}</td></tr>");
        sb.AppendLine($"                <tr><td class=\"label\">Error Occurred At:</td><td>{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC</td></tr>");
        sb.AppendLine("            </table>");
        sb.AppendLine("            <div class=\"error-message\">");
        sb.AppendLine("                <strong>Error Details:</strong><br>");
        sb.AppendLine($"                {WebUtility.HtmlEncode(errorMessage)}");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <p>Please investigate this issue and ensure the file format is correct before reprocessing.</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"footer\">");
        sb.AppendLine("            <p>This is an automated notification from EInvoice Integrator Background Service.</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private string GenerateSummaryEmailBody(ProcessingStatistics statistics)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"utf-8\">");
        sb.AppendLine("    <title>Processing Summary Report</title>");
        sb.AppendLine("    <style>");
        sb.AppendLine("        body { font-family: Arial, sans-serif; margin: 20px; color: #333; }");
        sb.AppendLine("        .container { max-width: 700px; margin: 0 auto; }");
        sb.AppendLine("        .header { background-color: #007bff; color: white; padding: 15px; text-align: center; }");
        sb.AppendLine("        .content { background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; }");
        sb.AppendLine("        .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; margin: 20px 0; }");
        sb.AppendLine("        .stat-card { background-color: white; padding: 15px; border: 1px solid #dee2e6; border-radius: 4px; text-align: center; }");
        sb.AppendLine("        .stat-number { font-size: 24px; font-weight: bold; color: #007bff; }");
        sb.AppendLine("        .stat-label { font-size: 14px; color: #6c757d; margin-top: 5px; }");
        sb.AppendLine("        .error-list { background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; margin: 15px 0; border-radius: 4px; }");
        sb.AppendLine("        .connection-stats { margin: 20px 0; }");
        sb.AppendLine("        .connection-table { width: 100%; border-collapse: collapse; margin: 10px 0; }");
        sb.AppendLine("        .connection-table th, .connection-table td { padding: 8px 12px; border-bottom: 1px solid #dee2e6; text-align: left; }");
        sb.AppendLine("        .connection-table th { background-color: #e9ecef; font-weight: bold; }");
        sb.AppendLine("        .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #6c757d; }");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");
        sb.AppendLine("        <div class=\"header\">");
        sb.AppendLine("            <h2>📊 Processing Summary Report</h2>");
        sb.AppendLine($"            <p>{statistics.ProcessingStartTime:yyyy-MM-dd HH:mm} - {statistics.ProcessingEndTime:yyyy-MM-dd HH:mm} UTC</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"content\">");
        
        // Statistics grid
        sb.AppendLine("            <div class=\"stats-grid\">");
        sb.AppendLine("                <div class=\"stat-card\">");
        sb.AppendLine($"                    <div class=\"stat-number\">{statistics.TotalFilesProcessed}</div>");
        sb.AppendLine("                    <div class=\"stat-label\">Total Files</div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("                <div class=\"stat-card\">");
        sb.AppendLine($"                    <div class=\"stat-number\" style=\"color: #28a745;\">{statistics.SuccessfulFiles}</div>");
        sb.AppendLine("                    <div class=\"stat-label\">Successful</div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("                <div class=\"stat-card\">");
        sb.AppendLine($"                    <div class=\"stat-number\" style=\"color: #dc3545;\">{statistics.ErrorFiles}</div>");
        sb.AppendLine("                    <div class=\"stat-label\">Failed</div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("                <div class=\"stat-card\">");
        sb.AppendLine($"                    <div class=\"stat-number\">{statistics.TotalProcessingTime.TotalMinutes:F1}m</div>");
        sb.AppendLine("                    <div class=\"stat-label\">Duration</div>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");

        // Connection statistics
        if (statistics.ConnectionStats?.Any() == true)
        {
            sb.AppendLine("            <div class=\"connection-stats\">");
            sb.AppendLine("                <h3>Files by Connection</h3>");
            sb.AppendLine("                <table class=\"connection-table\">");
            sb.AppendLine("                    <thead>");
            sb.AppendLine("                        <tr><th>Connection ID</th><th>Files Processed</th></tr>");
            sb.AppendLine("                    </thead>");
            sb.AppendLine("                    <tbody>");
            foreach (var connectionStat in statistics.ConnectionStats)
            {
                sb.AppendLine($"                        <tr><td>{connectionStat.Key}</td><td>{connectionStat.Value}</td></tr>");
            }
            sb.AppendLine("                    </tbody>");
            sb.AppendLine("                </table>");
            sb.AppendLine("            </div>");
        }

        // Error summary
        if (statistics.ErrorSummary?.Any() == true)
        {
            sb.AppendLine("            <div class=\"error-list\">");
            sb.AppendLine("                <h3>⚠️ Error Summary</h3>");
            sb.AppendLine("                <ul>");
            foreach (var error in statistics.ErrorSummary.Take(10)) // Limit to first 10 errors
            {
                sb.AppendLine($"                    <li>{WebUtility.HtmlEncode(error)}</li>");
            }
            if (statistics.ErrorSummary.Count > 10)
            {
                sb.AppendLine($"                    <li><em>... and {statistics.ErrorSummary.Count - 10} more errors</em></li>");
            }
            sb.AppendLine("                </ul>");
            sb.AppendLine("            </div>");
        }

        sb.AppendLine("        </div>");
        sb.AppendLine("        <div class=\"footer\">");
        sb.AppendLine("            <p>This is an automated summary from EInvoice Integrator Background Service.</p>");
        sb.AppendLine("        </div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    #endregion
}