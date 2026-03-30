namespace AegisEInvoicing.Paystack.Configuration;

public class PaystackOptions
{
    public const string SectionName = "Paystack";

    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.paystack.co";
    public string CallbackUrl { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
