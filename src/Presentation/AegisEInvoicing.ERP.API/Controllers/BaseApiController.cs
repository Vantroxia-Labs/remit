using AegisEInvoicing.Application.Common.Models;
using AegisEInvoicing.ERP.API.Models;
using AegisEInvoicing.ERP.API.Services;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AegisEInvoicing.ERP.API.Controllers;

/// <summary>
/// Base API controller with common functionality
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
public abstract class BaseApiController : ControllerBase
{
    private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    /// <summary>
    /// Creates a success response
    /// </summary>
    protected IActionResult Success<T>(T data, string? message = null)
    {
        return Ok(new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message ?? "Request completed successfully",
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a created response
    /// </summary>
    protected IActionResult Created<T>(T data, string location)
    {
        return base.Created(location, new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = "Resource created successfully",
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a no content response
    /// </summary>
    protected IActionResult NoContent(string? message = null)
    {
        return Ok(new ApiResponse<object>
        {
            Success = true,
            Message = message ?? "Request completed successfully",
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }

    protected IActionResult GenerateInvoiceFile(string data, string qrCode)
    {
        var pdfBytes = InvoicePdfGenerator.GenerateInvoicePdf(data, qrCode);
        return File(pdfBytes, "application/pdf", "invoice.pdf");
    }

    /// <summary>
    /// Creates an error response with detailed message
    /// </summary>
    protected IActionResult Error(string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message,
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a generic response (backward compatible)
    /// </summary>
    protected IActionResult GenericResponse(string message, bool isSuccess, object? data = null, int statusCode = StatusCodes.Status400BadRequest)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = isSuccess,
            Message = message,
            Data = data,
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Creates a paginated response
    /// </summary>
    protected IActionResult Paginated<T>(PaginatedList<T> result, string? message = null)
    {
        Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(new
        {
            result.TotalCount,
            result.PageSize,
            result.PageNumber,
            result.TotalPages,
            result.HasPreviousPage,
            result.HasNextPage
        }));

        return Ok(new ApiResponse<IEnumerable<T>>
        {
            Success = true,
            Data = result.Items,
            Message = message,
            TraceId = HttpContext.TraceIdentifier,
            Timestamp = DateTime.UtcNow
        });
    }
}