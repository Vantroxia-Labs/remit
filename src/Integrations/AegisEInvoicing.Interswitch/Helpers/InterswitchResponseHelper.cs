using System.Text.Json;
using AegisEInvoicing.Interswitch.Converters;
using AegisEInvoicing.Interswitch.Models;

namespace AegisEInvoicing.Interswitch.Helpers;

/// <summary>
/// Helper methods for deserializing Interswitch API responses
/// </summary>
public static class InterswitchResponseHelper
{
    /// <summary>
    /// Deserializes an Interswitch API response that may contain stringified JSON in the data field
    /// </summary>
    /// <typeparam name="T">The type of the inner data object</typeparam>
    /// <param name="response">The HTTP response message</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Deserialized Interswitch wrapped response</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails</exception>
    public static async Task<InterswitchWrappedResponse<T>?> DeserializeInterswitchResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken = default) where T : class
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = false
        };

        // Add converter for handling stringified JSON in data fields
        jsonOptions.Converters.Add(new InterswitchResponseConverterFactory());

        return JsonSerializer.Deserialize<InterswitchWrappedResponse<T>>(json, jsonOptions);
    }

    /// <summary>
    /// Deserializes an Interswitch API response from a JSON string
    /// </summary>
    /// <typeparam name="T">The type of the inner data object</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>Deserialized Interswitch wrapped response</returns>
    /// <exception cref="JsonException">Thrown when deserialization fails</exception>
    public static InterswitchWrappedResponse<T>? DeserializeInterswitchResponse<T>(
        string json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = false
        };

        // Add converter for handling stringified JSON in data fields
        jsonOptions.Converters.Add(new InterswitchResponseConverterFactory());

        return JsonSerializer.Deserialize<InterswitchWrappedResponse<T>>(json, jsonOptions);
    }

    /// <summary>
    /// Deserializes a direct Interswitch response (not wrapped)
    /// </summary>
    /// <typeparam name="T">The type of the data object</typeparam>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>Deserialized Interswitch response</returns>
    public static InterswitchResponse<T>? DeserializeDirectResponse<T>(
        string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = false
        };

        // Add converter for handling stringified JSON
        jsonOptions.Converters.Add(new InterswitchResponseConverterFactory());

        return JsonSerializer.Deserialize<InterswitchResponse<T>>(json, jsonOptions);
    }
}