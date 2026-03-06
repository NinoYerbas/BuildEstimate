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

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public abstract class BaseApiController : ControllerBase
{
    protected IActionResult Success<T>(T data, string? message = null)
    {
        return base.Ok(new ApiResponse<T>
        {
            Success = true,
            Message = message ?? "Operation completed successfully",
            Data = data
        });
    }

    protected IActionResult Error(string message, int statusCode = 400)
    {
        return StatusCode(statusCode, new ApiResponse<object>
        {
            Success = false,
            Message = message,
            Data = null
        });
    }

    protected string? GetCurrentUserId()
    {
        return User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    }

    protected string? GetCurrentUsername()
    {
        return User?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
    }

    protected new IActionResult Ok<T>(T data)
    {
        return base.Ok(new { success = true, data });
    }

    protected IActionResult Created<T>(T data)
    {
        return StatusCode(201, new { success = true, data });
    }

    protected new IActionResult BadRequest(string message)
    {
        return base.BadRequest(new { success = false, error = message });
    }

    protected new IActionResult NotFound(string message)
    {
        return base.NotFound(new { success = false, error = message });
    }
}

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}
