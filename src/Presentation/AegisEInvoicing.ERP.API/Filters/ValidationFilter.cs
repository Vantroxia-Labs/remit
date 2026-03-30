using AegisEInvoicing.ERP.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AegisEInvoicing.ERP.API.Filters;

/// <summary>
/// Model validation filter
/// </summary>
public sealed class ValidationFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            var errorMessages = errors.Select(e => $"{e.Key}: {string.Join(", ", e.Value)}");
            var response = new ApiResponse<object>
            {
                Success = false,
                Message = $"Validation failed: {string.Join("; ", errorMessages)}",
                Data = errors
            };

            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed for validation
    }
}
