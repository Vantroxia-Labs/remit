using System.Text;

namespace AegisEInvoicing.Application.Services;

public static class PublicKeyHelper
{
    public static string ToPem(string publicKeyBase64)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(publicKeyBase64);

        // Decode the Base64 string once
        var pemBytes = Convert.FromBase64String(publicKeyBase64);

        // Convert decoded bytes to text
        var pemText = Encoding.UTF8.GetString(pemBytes);

        return pemText;
    }
}