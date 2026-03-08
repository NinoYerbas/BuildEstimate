/*
 * BuildEstimate — Construction Estimating Software
 * Copyright (c) 2026 Julio Cesar Mendez Tobar. All Rights Reserved.
 */

// ============================================================================
// BASE API CONTROLLER — Shared Functionality For All Controllers
// ============================================================================
//
// This is IDENTICAL to your JERP BaseApiController.
// Every controller in BuildEstimate inherits from this.
//
// It provides:
//   - Ok() → HTTP 200 (success with data)
//   - Created() → HTTP 201 (successfully created)
//   - BadRequest() → HTTP 400 (invalid input)
//   - NotFound() → HTTP 404 (doesn't exist)
//   - Error() → any error status code
//   - GetCurrentUserId() → who's logged in (from JWT)
//
// DESIGN PATTERN — "Template Method":
//   By putting common methods in a base class, every controller
//   automatically gets them. No code duplication.
// ============================================================================

using Microsoft.AspNetCore.Mvc;

namespace BuildEstimate.Api.Controllers;

/// <summary>
/// Abstract base class that every API controller in BuildEstimate inherits from.
/// Provides standardized helper methods for returning consistent JSON responses.
///
/// All responses follow the <see cref="ApiResponse{T}"/> envelope format:
///   { "success": true/false, "message": "...", "data": { ... } }
///
/// WHY A BASE CLASS?
///   If you didn't have this, every controller would repeat the same response-building code.
///   The base class puts it in one place — change here, all controllers get the update.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Returns HTTP 200 with the data wrapped in an ApiResponse envelope.
    /// Use this for any successful GET or PUT operation.
    /// </summary>
    /// <typeparam name="T">The type of data being returned.</typeparam>
    /// <param name="data">The payload to include in the response body.</param>
    /// <param name="message">Optional success message. Defaults to "Operation completed successfully".</param>
    protected IActionResult Success<T>(T data, string? message = null)
    {
        return base.Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Operation completed successfully",
            Data = data
        });
    }

    /// <summary>
    /// Returns an error response with the given HTTP status code.
    /// Use this for unexpected failures or business rule violations.
    /// </summary>
    /// <param name="message">A human-readable description of what went wrong.</param>
    /// <param name="statusCode">The HTTP status code to return. Defaults to 400 (Bad Request).</param>
    protected IActionResult Error(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        });
    }

    /// <summary>
    /// Gets the ID of the currently authenticated user from their JWT token.
    /// Returns null if the user is not authenticated (e.g., AllowAnonymous endpoints).
    /// </summary>
    protected string? GetCurrentUserId()
    {
        return User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value; // ← reads the "sub" claim from the JWT
    }

    /// <summary>
    /// Gets the username of the currently authenticated user from their JWT token.
    /// Used for audit trail fields like CreatedBy and UpdatedBy.
    /// Returns null if not authenticated.
    /// </summary>
    protected string? GetCurrentUsername()
    {
        return User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value; // ← reads the "name" claim from the JWT
    }

    /// <summary>
    /// Returns HTTP 200 with a simple { success: true, data: ... } response.
    /// Hides the base ControllerBase.Ok() to enforce the consistent response format.
    /// </summary>
    /// <typeparam name="T">The type of data being returned.</typeparam>
    /// <param name="data">The payload to return in the response body.</param>
    protected new IActionResult Ok<T>(T data)
    {
        return base.Ok(new { success = true, data });
    }

    /// <summary>
    /// Returns HTTP 201 (Created) for resources that were successfully created.
    /// Use this instead of Ok() when a new database record was just inserted.
    /// HTTP 201 is the correct status for POST requests that create resources.
    /// </summary>
    /// <typeparam name="T">The type of the newly created resource.</typeparam>
    /// <param name="data">The newly created resource to return.</param>
    protected IActionResult Created<T>(T data)
    {
        return StatusCode(201, new { success = true, data }); // ← 201 = "I created something new"
    }

    /// <summary>
    /// Returns HTTP 400 (Bad Request) for invalid user input.
    /// Hides the base ControllerBase.BadRequest() to enforce consistent error format.
    /// Use this when the request body fails validation or references non-existent records.
    /// </summary>
    /// <param name="message">Description of what was invalid about the request.</param>
    protected new IActionResult BadRequest(string message)
    {
        return base.BadRequest(new { success = false, error = message });
    }

    /// <summary>
    /// Returns HTTP 404 (Not Found) when a requested resource doesn't exist.
    /// Hides the base ControllerBase.NotFound() to enforce consistent error format.
    /// </summary>
    /// <param name="message">Description of what was not found, e.g., "Project with ID X not found".</param>
    protected new IActionResult NotFound(string message)
    {
        return base.NotFound(new { success = false, error = message });
    }
}

/// <summary>
/// Standard envelope that wraps every API response in BuildEstimate.
/// Every endpoint returns this shape, making it easy for the frontend to handle responses uniformly.
///
/// Success response:  { "success": true,  "message": "Created successfully", "data": { ... } }
/// Error response:    { "success": false, "message": "Not found",             "data": null }
/// </summary>
/// <typeparam name="T">The type of the payload in the Data field.</typeparam>
public class ApiResponse<T>
{
    /// <summary>True if the operation succeeded, false if it failed.</summary>
    public bool Success { get; set; }

    /// <summary>A human-readable message explaining the result, suitable for display in the UI.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>The response payload. Null when Success is false.</summary>
    public T? Data { get; set; }
}
