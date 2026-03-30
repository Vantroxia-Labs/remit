using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Domain.Extensions;

/// <summary>
/// JSON converter for <see cref="TimeOnly"/> values (nullable),
/// enforcing strict "HH:mm:ss" 24-hour formatting.
/// </summary>
public sealed class TimeOnlyJsonConverter : JsonConverter<TimeOnly?>
{
    private const string Format = "HH:mm:ss"; // enforce 24h format
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing TimeOnly. Expected {JsonTokenType.String}, got {reader.TokenType}.");
        }

        var str = reader.GetString();
        if (string.IsNullOrWhiteSpace(str))
            return null;

        if (TimeOnly.TryParseExact(str, Format, InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        throw new JsonException($"Invalid time format. Expected format is '{Format}', but got '{str}'.");
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString(Format, InvariantCulture));
    }
}