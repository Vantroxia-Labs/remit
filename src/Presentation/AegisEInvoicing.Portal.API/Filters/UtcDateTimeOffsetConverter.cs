using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Portal.API.Filters
{
    public class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
    {
        public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var value = reader.GetString();
            if (string.IsNullOrWhiteSpace(value))
                return null;

            // Always treat parsed date as UTC
            return DateTimeOffset.Parse(value, null, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                // Always write as UTC ISO 8601
                writer.WriteStringValue(value.Value.UtcDateTime.ToString("o"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
}