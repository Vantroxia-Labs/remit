using System.Text.Json;
using System.Text.Json.Serialization;

namespace AegisEInvoicing.Interswitch.Converters;

/// <summary>
/// Handles JSON properties that may be returned as stringified JSON blocks.
/// Automatically detects if the value is a string and deserializes it, or processes it as a normal object.
/// </summary>
/// <typeparam name="T">The target type to deserialize to</typeparam>
public class NestedJsonStringConverter<T> : JsonConverter<T> where T : class
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Handle null values
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // If the token is a string, it's a stringified JSON - deserialize it
        if (reader.TokenType == JsonTokenType.String)
        {
            var jsonString = reader.GetString();

            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return null;
            }

            try
            {
                // Create new options to avoid infinite recursion
                var innerOptions = new JsonSerializerOptions(options);

                // Remove this converter from inner options to prevent infinite loop
                innerOptions.Converters.Clear();
                foreach (var converter in options.Converters)
                {
                    if (converter.GetType() != typeof(NestedJsonStringConverter<T>))
                    {
                        innerOptions.Converters.Add(converter);
                    }
                }

                return JsonSerializer.Deserialize<T>(jsonString, innerOptions);
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Failed to deserialize nested JSON string for type {typeof(T).Name}", ex);
            }
        }

        // If it's already an object/array, deserialize normally
        if (reader.TokenType == JsonTokenType.StartObject || reader.TokenType == JsonTokenType.StartArray)
        {
            // Create new options without this converter to prevent recursion
            var innerOptions = new JsonSerializerOptions(options);
            innerOptions.Converters.Clear();
            foreach (var converter in options.Converters)
            {
                if (converter.GetType() != typeof(NestedJsonStringConverter<T>))
                {
                    innerOptions.Converters.Add(converter);
                }
            }

            // Read the entire object/array as JsonDocument
            using var doc = JsonDocument.ParseValue(ref reader);
            var json = doc.RootElement.GetRawText();
            return JsonSerializer.Deserialize<T>(json, innerOptions);
        }

        throw new JsonException($"Unexpected token type {reader.TokenType} when deserializing {typeof(T).Name}");
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Create new options without this converter
        var innerOptions = new JsonSerializerOptions(options);
        innerOptions.Converters.Clear();
        foreach (var converter in options.Converters)
        {
            if (converter.GetType() != typeof(NestedJsonStringConverter<T>))
            {
                innerOptions.Converters.Add(converter);
            }
        }

        // Serialize normally (not as a string)
        JsonSerializer.Serialize(writer, value, innerOptions);
    }
}