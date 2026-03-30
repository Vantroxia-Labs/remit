using AegisEInvoicing.ERP.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Reflection;
using System.Text.Json;

namespace AegisEInvoicing.ERP.API.Filters;

/// <summary>
/// Action filter that validates incoming JSON requests against the expected model properties.
/// Rejects requests containing unknown/undeclared properties to prevent BOPLA vulnerabilities.
/// (Broken Object Property Level Authorization)
/// </summary>
public sealed class UnknownPropertiesValidationFilter : IAsyncActionFilter
{
    private readonly ILogger<UnknownPropertiesValidationFilter> _logger;

    public UnknownPropertiesValidationFilter(ILogger<UnknownPropertiesValidationFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Only validate POST, PUT, PATCH requests with JSON body
        var method = context.HttpContext.Request.Method;
        if (!IsModifyingRequest(method))
        {
            await next();
            return;
        }

        var contentType = context.HttpContext.Request.ContentType;
        if (string.IsNullOrEmpty(contentType) || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // Get the action descriptor to find the expected parameter type
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            await next();
            return;
        }

        // Find the parameter that receives the request body (marked with [FromBody] or first complex type)
        var bodyParameter = FindBodyParameter(actionDescriptor);
        if (bodyParameter == null)
        {
            await next();
            return;
        }

        // Enable buffering so we can read the request body multiple times
        context.HttpContext.Request.EnableBuffering();

        try
        {
            // Read the raw JSON from request body
            context.HttpContext.Request.Body.Position = 0;
            using var reader = new StreamReader(context.HttpContext.Request.Body, leaveOpen: true);
            var rawJson = await reader.ReadToEndAsync();
            context.HttpContext.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                await next();
                return;
            }

            // Parse the JSON and get all property names from the request
            var requestProperties = GetJsonPropertyNames(rawJson);
            if (requestProperties == null || requestProperties.Count == 0)
            {
                await next();
                return;
            }

            // Get allowed property names from the expected model type
            var allowedProperties = GetAllowedPropertyNames(bodyParameter.ParameterType);

            // Find any unknown properties
            var unknownProperties = requestProperties
                .Where(p => !allowedProperties.Contains(p, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (unknownProperties.Count > 0)
            {
                _logger.LogWarning(
                    "Request to {Path} contains unknown properties: {UnknownProperties}. " +
                    "Allowed properties: {AllowedProperties}",
                    context.HttpContext.Request.Path,
                    string.Join(", ", unknownProperties),
                    string.Join(", ", allowedProperties));

                var response = new ApiResponse<object>
                {
                    Success = false,
                    Message = $"Request contains unknown properties: {string.Join(", ", unknownProperties)}. " +
                              $"Allowed properties are: {string.Join(", ", allowedProperties)}",
                    Data = new { UnknownProperties = unknownProperties, AllowedProperties = allowedProperties },
                    DeveloperMessage = $"The following properties are not allowed: {string.Join(", ", unknownProperties)}. " +
                                      $"Allowed properties are: {string.Join(", ", allowedProperties)}"
                };

                context.Result = new BadRequestObjectResult(response);
                return;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON for unknown property validation");
            // Let the model binding handle invalid JSON
        }

        await next();
    }

    private static bool IsModifyingRequest(string method)
    {
        return method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PUT", StringComparison.OrdinalIgnoreCase) ||
               method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
    }

    private static ParameterInfo? FindBodyParameter(ControllerActionDescriptor actionDescriptor)
    {
        var methodInfo = actionDescriptor.MethodInfo;
        var parameters = methodInfo.GetParameters();

        // First, look for parameter with [FromBody] attribute
        var fromBodyParam = parameters.FirstOrDefault(p =>
            p.GetCustomAttribute<Microsoft.AspNetCore.Mvc.FromBodyAttribute>() != null);

        if (fromBodyParam != null)
        {
            return fromBodyParam;
        }

        // Otherwise, find the first complex type parameter (not primitive, string, or common types)
        return parameters.FirstOrDefault(p => IsComplexType(p.ParameterType));
    }

    private static bool IsComplexType(Type type)
    {
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
            type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
            type == typeof(Guid) || type == typeof(TimeSpan))
        {
            return false;
        }

        // Check for nullable primitives
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return IsComplexType(underlyingType);
        }

        // Exclude common simple types
        if (type == typeof(CancellationToken))
        {
            return false;
        }

        return type.IsClass || type.IsValueType;
    }

    private static HashSet<string>? GetJsonPropertyNames(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectPropertyNames(document.RootElement, properties, "");
            return properties;
        }
        catch
        {
            return null;
        }
    }

    private static void CollectPropertyNames(JsonElement element, HashSet<string> properties, string prefix)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                var fullPath = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                properties.Add(fullPath);

                // Recursively collect nested properties
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    CollectPropertyNames(property.Value, properties, fullPath);
                }
                else if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    // For arrays, check if first element is an object and collect its properties
                    foreach (var arrayElement in property.Value.EnumerateArray())
                    {
                        if (arrayElement.ValueKind == JsonValueKind.Object)
                        {
                            CollectPropertyNames(arrayElement, properties, fullPath);
                            break; // Only check first element for property names
                        }
                    }
                }
            }
        }
    }

    private static HashSet<string> GetAllowedPropertyNames(Type type)
    {
        var properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectAllowedPropertyNames(type, properties, "", new HashSet<Type>());
        return properties;
    }

    private static void CollectAllowedPropertyNames(Type type, HashSet<string> properties, string prefix, HashSet<Type> visitedTypes)
    {
        // Prevent infinite recursion for circular references
        if (visitedTypes.Contains(type))
        {
            return;
        }

        visitedTypes.Add(type);

        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            type = underlyingType;
        }

        // Get all public properties
        var typeProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in typeProperties)
        {
            // Check for JsonPropertyName attribute
            var jsonPropertyAttr = prop.GetCustomAttribute<System.Text.Json.Serialization.JsonPropertyNameAttribute>();
            var propertyName = jsonPropertyAttr?.Name ?? prop.Name;

            var fullPath = string.IsNullOrEmpty(prefix) ? propertyName : $"{prefix}.{propertyName}";
            properties.Add(fullPath);

            // Also add the original property name if different (for case-insensitive matching)
            if (jsonPropertyAttr != null && !propertyName.Equals(prop.Name, StringComparison.OrdinalIgnoreCase))
            {
                var altPath = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                properties.Add(altPath);
            }

            // Recursively collect nested complex type properties
            var propType = prop.PropertyType;

            // Handle collections
            if (propType.IsGenericType)
            {
                var genericArgs = propType.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    var elementType = genericArgs[^1]; // Last generic argument (for dictionaries, get value type)
                    if (IsComplexType(elementType) && !IsPrimitiveOrString(elementType))
                    {
                        CollectAllowedPropertyNames(elementType, properties, fullPath, visitedTypes);
                    }
                }
            }
            else if (propType.IsArray)
            {
                var elementType = propType.GetElementType();
                if (elementType != null && IsComplexType(elementType) && !IsPrimitiveOrString(elementType))
                {
                    CollectAllowedPropertyNames(elementType, properties, fullPath, visitedTypes);
                }
            }
            else if (IsComplexType(propType) && !IsPrimitiveOrString(propType))
            {
                CollectAllowedPropertyNames(propType, properties, fullPath, visitedTypes);
            }
        }

        visitedTypes.Remove(type);
    }

    private static bool IsPrimitiveOrString(Type type)
    {
        return type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
               type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
               type == typeof(Guid) || type == typeof(TimeSpan) ||
               type == typeof(DateOnly) || type == typeof(TimeOnly);
    }
}
