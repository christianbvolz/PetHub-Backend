using Microsoft.AspNetCore.Mvc;
using PetHub.API.DTOs.Common;

namespace PetHub.API.Common;

/// <summary>
/// Base controller that provides standardized API response methods.
/// All API controllers should inherit from this class to ensure consistent response formatting.
/// </summary>
/// <remarks>
/// This class wraps all controller responses in the ApiResponse&lt;T&gt; structure, providing:
/// - Consistent JSON format across all endpoints (success, data, message, errors, timestamp)
/// - Reduced verbosity in controller methods
/// - Type-safe response handling with generics
/// - Centralized response logic for easier maintenance
/// </remarks>
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a successful response (HTTP 200) with optional data and message.
    /// </summary>
    protected OkObjectResult Success<T>(T? data = default, string? message = null)
    {
        return Ok(ApiResponse<T>.SuccessResponse(data, message));
    }

    /// <summary>
    /// Returns a bad request response (HTTP 400) with one or more error messages.
    /// </summary>
    protected BadRequestObjectResult Error(params string[] errors)
    {
        return BadRequest(ApiResponse<object>.ErrorResponse(errors));
    }

    /// <summary>
    /// Returns an unauthorized response (HTTP 401) with one or more error messages.
    /// </summary>
    protected UnauthorizedObjectResult Unauthorized(params string[] errors)
    {
        return base.Unauthorized(ApiResponse<object>.ErrorResponse(errors));
    }

    /// <summary>
    /// Returns a not found response (HTTP 404) with one or more error messages.
    /// </summary>
    protected NotFoundObjectResult NotFound(params string[] errors)
    {
        return base.NotFound(ApiResponse<object>.ErrorResponse(errors));
    }

    /// <summary>
    /// Returns a created response (HTTP 201) with location header and data.
    /// </summary>
    protected CreatedAtActionResult CreatedAtAction<T>(
        string actionName,
        object routeValues,
        T data
    )
    {
        return base.CreatedAtAction(actionName, routeValues, ApiResponse<T>.SuccessResponse(data));
    }
}
