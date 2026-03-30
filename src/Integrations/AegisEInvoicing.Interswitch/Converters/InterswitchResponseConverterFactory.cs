using System.Text.Json;
using System.Text.Json.Serialization;
using AegisEInvoicing.Interswitch.Models;

namespace AegisEInvoicing.Interswitch.Converters;

/// <summary>
/// Factory for creating converters for InterswitchResponse types
/// Handles nested JSON strings in the data field
/// </summary>
public class InterswitchResponseConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        // Check if the type itself is InterswitchResponse<T>
        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(InterswitchResponse<>))
        {
            return true;
        }

        // Check if the type derives from InterswitchResponse<T>
        var baseType = typeToConvert.BaseType;
        while (baseType != null)
        {
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(InterswitchResponse<>))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        // Find the InterswitchResponse<T> in the type hierarchy
        Type? interswitchResponseType = null;

        if (typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(InterswitchResponse<>))
        {
            interswitchResponseType = typeToConvert;
        }
        else
        {
            var baseType = typeToConvert.BaseType;
            while (baseType != null)
            {
                if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(InterswitchResponse<>))
                {
                    interswitchResponseType = baseType;
                    break;
                }
                baseType = baseType.BaseType;
            }
        }

        if (interswitchResponseType == null)
        {
            throw new InvalidOperationException($"Type {typeToConvert} does not inherit from InterswitchResponse<T>");
        }

        var innerType = interswitchResponseType.GetGenericArguments()[0];
        var converterType = typeof(InterswitchResponseConverter<>).MakeGenericType(innerType);

        return (JsonConverter?)Activator.CreateInstance(converterType, typeToConvert);
    }
}

/// <summary>
/// Converter for InterswitchResponse that handles stringified JSON in data field
/// </summary>
internal class InterswitchResponseConverter<T> : JsonConverter<InterswitchResponse<T>>
{
    private readonly Type _targetType;

    public InterswitchResponseConverter(Type targetType)
    {
        _targetType = targetType;
    }

    public override InterswitchResponse<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        // If it's a string, parse it first to get the actual JSON
        if (reader.TokenType == JsonTokenType.String)
        {
            var jsonString = reader.GetString();
            if (string.IsNullOrWhiteSpace(jsonString))
            {
                return null;
            }

            // Parse the string to get a JsonDocument
            using var stringDoc = JsonDocument.Parse(jsonString);
            return DeserializeFromElement(stringDoc.RootElement, typeToConvert, options);
        }

        // Otherwise, read the current object
        using var doc = JsonDocument.ParseValue(ref reader);
        return DeserializeFromElement(doc.RootElement, typeToConvert, options);
    }

    private InterswitchResponse<T> DeserializeFromElement(JsonElement element, Type typeToConvert, JsonSerializerOptions options)
    {
        // Create an instance of the target type (could be derived class)
        var response = (InterswitchResponse<T>)Activator.CreateInstance(typeToConvert)!;

        // Try both lowercase and original casing for property names
        if (TryGetPropertyCaseInsensitive(element, "code", out var codeElement))
        {
            response.Code = codeElement.GetInt32();
        }

        if (TryGetPropertyCaseInsensitive(element, "message", out var messageElement))
        {
            response.Message = messageElement.GetString();
        }

        if (TryGetPropertyCaseInsensitive(element, "success", out var successElement))
        {
            response.Success = successElement.GetBoolean();
        }

        if (TryGetPropertyCaseInsensitive(element, "data", out var dataElement))
        {
            if (dataElement.ValueKind != JsonValueKind.Null)
            {
                response.Data = JsonSerializer.Deserialize<T>(dataElement.GetRawText(), options);
            }
        }

        if (TryGetPropertyCaseInsensitive(element, "error", out var errorElement))
        {
            if (errorElement.ValueKind != JsonValueKind.Null)
            {
                response.ErrorDetails = JsonSerializer.Deserialize<InterswitchErrorDetails>(errorElement.GetRawText(), options);
            }
        }

        return response;
    }

    private static bool TryGetPropertyCaseInsensitive(JsonElement element, string propertyName, out JsonElement value)
    {
        // Try exact match first
        if (element.TryGetProperty(propertyName, out value))
        {
            return true;
        }

        // Try case-insensitive search
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }

    public override void Write(Utf8JsonWriter writer, InterswitchResponse<T> value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        JsonSerializer.Serialize(writer, value, options);
    }
}