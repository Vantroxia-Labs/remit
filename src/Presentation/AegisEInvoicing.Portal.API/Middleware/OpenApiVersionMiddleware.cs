using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AegisEInvoicing.Portal.API.Middleware;

/// <summary>
/// Middleware to modify OpenAPI version in Swagger JSON to ensure IBM API Connect compatibility
/// </summary>
public class OpenApiVersionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OpenApiVersionMiddleware> _logger;

    public OpenApiVersionMiddleware(RequestDelegate next, ILogger<OpenApiVersionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a swagger.json request
        if (context.Request.Path.Value?.Contains("/swagger", StringComparison.OrdinalIgnoreCase) == true &&
            context.Request.Path.Value?.EndsWith(".json", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogInformation("Intercepting Swagger JSON request: {Path}", context.Request.Path);
            
            // Buffer the response
            var originalBodyStream = context.Response.Body;
            
            using (var responseBody = new MemoryStream())
            {
                context.Response.Body = responseBody;

                // Process the request pipeline
                await _next(context);

                // Read and modify the response
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                
                // Modify the OpenAPI version
                var modifiedText = ModifyOpenApiVersion(responseText);
                
                // Calculate content length
                var byteArray = Encoding.UTF8.GetBytes(modifiedText);
                context.Response.ContentLength = byteArray.Length;
                
                // Write the modified response to the original stream
                context.Response.Body = originalBodyStream;
                await context.Response.Body.WriteAsync(byteArray, 0, byteArray.Length);
            }
        }
        else
        {
            await _next(context);
        }
    }

    private string ModifyOpenApiVersion(string jsonContent)
    {
        try
        {
            var jsonNode = JsonNode.Parse(jsonContent);
            if (jsonNode != null)
            {
                // Change OpenAPI version to 3.0.3
                if (jsonNode["openapi"] != null)
                {
                    var currentVersion = jsonNode["openapi"]?.ToString();
                    _logger.LogInformation("Changing OpenAPI version from {CurrentVersion} to 3.0.3", currentVersion);
                    jsonNode["openapi"] = "3.0.3";
                }
                
                return jsonNode.ToJsonString(new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to modify OpenAPI version");
        }
        
        return jsonContent;
    }
}