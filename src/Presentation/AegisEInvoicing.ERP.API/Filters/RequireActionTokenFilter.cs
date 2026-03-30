using AegisEInvoicing.ERP.API.Models;
using AegisEInvoicing.ERP.API.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace AegisEInvoicing.ERP.API.Filters;

/// <summary>
/// Filter attribute to require action token validation for state-changing operations.
/// Addresses VAPT finding: Response tampering - ensures server-side validation for all actions.
///
/// Usage:
/// [RequireActionToken("ApproveInvoice", ResourceIdParameter = "invoiceId")]
/// public async Task&lt;IActionResult&gt; ApproveInvoice(Guid invoiceId, [FromBody] ApprovalRequest request)
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireActionTokenAttribute : Attribute, IFilterFactory
{
    /// <summary>
    /// The expected action type for validation
    /// </summary>
    public string ActionType { get; }

    /// <summary>
    /// Name of the route/query parameter containing the resource ID
    /// </summary>
    public string ResourceIdParameter { get; set; } = "id";

    /// <summary>
    /// Name of the header containing the action token
    /// </summary>
    public string TokenHeaderName { get; set; } = "X-Action-Token";

    /// <summary>
    /// Whether the action token can also be passed in the request body
    /// </summary>
    public bool AllowTokenInBody { get; set; } = true;

    /// <summary>
    /// Name of the body property containing the action token (if AllowTokenInBody is true)
    /// </summary>
    public string TokenBodyProperty { get; set; } = "actionToken";

    public bool IsReusable => false;

    public RequireActionTokenAttribute(string actionType)
    {
        ActionType = actionType;
    }

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var validator = serviceProvider.GetRequiredService<IServerSideActionValidator>();
        var logger = serviceProvider.GetRequiredService<ILogger<RequireActionTokenFilter>>();

        return new RequireActionTokenFilter(
            validator,
            logger,
            ActionType,
            ResourceIdParameter,
            TokenHeaderName,
            AllowTokenInBody,
            TokenBodyProperty);
    }
}

/// <summary>
/// The actual filter implementation
/// </summary>
public class RequireActionTokenFilter : IAsyncActionFilter
{
    private readonly IServerSideActionValidator _validator;
    private readonly ILogger<RequireActionTokenFilter> _logger;
    private readonly string _actionType;
    private readonly string _resourceIdParameter;
    private readonly string _tokenHeaderName;
    private readonly bool _allowTokenInBody;
    private readonly string _tokenBodyProperty;

    public RequireActionTokenFilter(
        IServerSideActionValidator validator,
        ILogger<RequireActionTokenFilter> logger,
        string actionType,
        string resourceIdParameter,
        string tokenHeaderName,
        bool allowTokenInBody,
        string tokenBodyProperty)
    {
        _validator = validator;
        _logger = logger;
        _actionType = actionType;
        _resourceIdParameter = resourceIdParameter;
        _tokenHeaderName = tokenHeaderName;
        _allowTokenInBody = allowTokenInBody;
        _tokenBodyProperty = tokenBodyProperty;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        // Extract action token from header or body
        var actionToken = ExtractActionToken(context);
        if (string.IsNullOrEmpty(actionToken))
        {
            _logger.LogWarning(
                "Action token required but not provided for {ActionType} at {Path}",
                _actionType, httpContext.Request.Path);

            context.Result = CreateErrorResult(
                "Action token is required for this operation",
                StatusCodes.Status400BadRequest);
            return;
        }

        // Extract resource ID from route/query parameters
        var resourceId = ExtractResourceId(context);
        if (string.IsNullOrEmpty(resourceId))
        {
            _logger.LogWarning(
                "Resource ID not found in parameter '{Parameter}' for {ActionType}",
                _resourceIdParameter, _actionType);

            context.Result = CreateErrorResult(
                $"Resource ID parameter '{_resourceIdParameter}' is required",
                StatusCodes.Status400BadRequest);
            return;
        }

        // Extract user ID from claims
        var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? httpContext.User.FindFirstValue("sub")
                     ?? httpContext.User.FindFirstValue("BusinessId")
                     ?? "anonymous";

        // Validate the action token
        var validationResult = _validator.ValidateActionToken(
            actionToken,
            _actionType,
            resourceId,
            userId);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Action token validation failed for {ActionType} on {ResourceId}: {Error}",
                _actionType, resourceId, validationResult.ErrorMessage);

            context.Result = CreateErrorResult(
                validationResult.ErrorMessage ?? "Invalid action token",
                StatusCodes.Status403Forbidden);
            return;
        }

        // Store validation result in HttpContext for use in the action
        httpContext.Items["ActionTokenValidation"] = validationResult;

        _logger.LogInformation(
            "Action token validated for {ActionType} on {ResourceId} by {UserId}",
            _actionType, resourceId, userId);

        // Execute the action
        var resultContext = await next();

        // If action succeeded, consume the token to prevent replay
        if (resultContext.Exception == null && validationResult.TokenId != null)
        {
            _validator.ConsumeToken(validationResult.TokenId);
        }
    }

    private string? ExtractActionToken(ActionExecutingContext context)
    {
        // Try header first
        if (context.HttpContext.Request.Headers.TryGetValue(_tokenHeaderName, out var headerValue) &&
            !string.IsNullOrEmpty(headerValue))
        {
            return headerValue.ToString();
        }

        // Try body if allowed
        if (_allowTokenInBody)
        {
            foreach (var argument in context.ActionArguments.Values)
            {
                if (argument == null) continue;

                var property = argument.GetType().GetProperty(_tokenBodyProperty);
                if (property != null)
                {
                    var value = property.GetValue(argument) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }

                // Also check for ActionToken property (common naming)
                var actionTokenProperty = argument.GetType().GetProperty("ActionToken");
                if (actionTokenProperty != null)
                {
                    var value = actionTokenProperty.GetValue(argument) as string;
                    if (!string.IsNullOrEmpty(value))
                    {
                        return value;
                    }
                }
            }
        }

        return null;
    }

    private string? ExtractResourceId(ActionExecutingContext context)
    {
        // Try action arguments first
        if (context.ActionArguments.TryGetValue(_resourceIdParameter, out var argValue) && argValue != null)
        {
            return argValue.ToString();
        }

        // Try route values
        if (context.RouteData.Values.TryGetValue(_resourceIdParameter, out var routeValue) && routeValue != null)
        {
            return routeValue.ToString();
        }

        // Try query string
        if (context.HttpContext.Request.Query.TryGetValue(_resourceIdParameter, out var queryValue) &&
            !string.IsNullOrEmpty(queryValue))
        {
            return queryValue.ToString();
        }

        return null;
    }

    private static IActionResult CreateErrorResult(string message, int statusCode)
    {
        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = new { ActionTokenError = message },
            TraceId = Guid.NewGuid().ToString()
        };

        return new ObjectResult(response)
        {
            StatusCode = statusCode
        };
    }
}
