using AegisEInvoicing.Portal.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AegisEInvoicing.Portal.API.Filters;

/// <summary>
/// Standardizes API responses
/// </summary>
public sealed class ApiResponseFilter : IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult &&
            objectResult.Value != null &&
            objectResult.Value.GetType() != typeof(ApiResponse<>) &&
            !objectResult.Value.GetType().IsGenericType)
        {
            var response = new ApiResponse<object>
            {
                Success = objectResult.StatusCode >= 200 && objectResult.StatusCode < 300,
                Data = objectResult.Value,
                Message = "Request completed"
            };

            context.Result = new ObjectResult(response)
            {
                StatusCode = objectResult.StatusCode
            };
        }

        await next();
    }
}