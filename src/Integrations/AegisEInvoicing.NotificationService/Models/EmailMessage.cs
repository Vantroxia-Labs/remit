namespace AegisEInvoicing.NotificationService.Models;

public class EmailMessage
{
    public string To { get; set; } = string.Empty;
    public List<string> ToAddresses { get; set; } = new();
    public List<string> CcAddresses { get; set; } = new();
    public List<string> BccAddresses { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string TextBody { get; set; } = string.Empty;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string? ReplyToEmail { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<EmailAttachment> Attachments { get; set; } = new();
}