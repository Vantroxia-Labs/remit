namespace AegisEInvoicing.Domain.Enums;

/// <summary>
/// Represents the authentication scheme used by an Access Point Provider.
/// This is a finite set of supported auth protocols — not the provider itself.
/// New providers reuse one of these schemes; new schemes require code changes.
/// </summary>
public enum AppAuthScheme
{
    /// <summary>
    /// OAuth 2.0 Client Credentials flow.
    /// Requires: ApiKey (ClientId) + ApiSecret (ClientSecret) + TokenEndpoint URL.
    /// Produces a Bearer token sent in the Authorization header.
    /// Example: Interswitch SwitchTax.
    /// </summary>
    OAuth2ClientCredentials = 1,

    /// <summary>
    /// Static API key sent in a configurable request header.
    /// Requires: ApiKey + ApiKeyHeaderName.
    /// Example: BlueBridge (X-API-Key header).
    /// </summary>
    StaticApiKey = 2,

    /// <summary>
    /// API key combined with an HMAC-SHA256 request signature in a second header.
    /// Requires: ApiKey (client key) + ApiSecret (signing secret) + ApiKeyHeaderName + SignatureHeaderName.
    /// Example: eTranzact (X-API-Key + X-API-Signature headers).
    /// </summary>
    HmacApiKey = 3
}
