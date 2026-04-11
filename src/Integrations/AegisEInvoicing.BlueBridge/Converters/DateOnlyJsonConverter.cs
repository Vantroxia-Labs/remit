using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.BlueBridge.Converters;

/// <summary>
/// JSON converter for <see cref="DateOnly"/> values (nullable),
/// enforcing strict "yyyy-MM-dd" formatting (ISO 8601 subset).
/// </summary>
public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    private const string Format = "yyyy-MM-dd";
    private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException(
                $"Unexpected token parsing DateOnly. Expected {JsonTokenType.String}, got {reader.TokenType}.");
        }

        var str = reader.GetString();
        if (string.IsNullOrWhiteSpace(str))
            return null;

        if (DateOnly.TryParseExact(str, Format, InvariantCulture, DateTimeStyles.None, out var parsed))
            return parsed;

        throw new JsonException($"Invalid date format. Expected '{Format}', but got '{str}'.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString(Format, InvariantCulture));
    }
}
