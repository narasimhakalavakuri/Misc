using Kiota.ApiClient.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PositionReporting.Api.Services.Interfaces;
using System.Security.Claims;

namespace PositionReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve a list of all system users.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanUserAdmin")] // User administration permission
    [ProducesResponseType(typeof(List<User>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<User>>> ListUsers(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] string? sort,
        [FromQuery] string? filter,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var users = await _userService.ListUsersAsync(new UserListRequest
        {
            Limit = limit,
            Offset = offset,
            Sort = sort,
            Filter = filter
        });
        return Ok(users);
    }

    /// <summary>
    /// Create a new system user.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanUserAdmin")]
    [ProducesResponseType(typeof(User), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<User>> CreateUser(
        [FromBody] UserCreateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var newUser = await _userService.CreateUserAsync(request);
        return CreatedAtAction(nameof(GetCurrentUser), new { userId = newUser.UserId }, newUser); // Adjust if there's a specific GetUserById
    }

    /// <summary>
    /// Update an existing user's department and access mask.
    /// </summary>
    [HttpPatch("{userId}")]
    [Authorize(Policy = "CanUserAdmin")]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<User>> UpdateUser(
        [FromRoute] string userId,
        [FromBody] UserUpdateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var updatedUser = await _userService.UpdateUserAsync(userId, request);
        if (updatedUser == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "User not found.", Details = $"User '{userId}' does not exist." });
        }
        return Ok(updatedUser);
    }

    /// <summary>
    /// Delete a system user.
    /// </summary>
    [HttpDelete("{userId}")]
    [Authorize(Policy = "CanUserAdmin")]
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> DeleteUser(
        [FromRoute] string userId,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        await _userService.DeleteUserAsync(userId);
        return NoContent();
    }

    /// <summary>
    /// Retrieve details of the authenticated user.
    /// </summary>
    [HttpGet("me")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [ProducesResponseType(typeof(User), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<User>> GetCurrentUser(
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var user = await _userService.GetUserByIdAsync(Guid.Parse(currentUserId)); // Assuming internal ID is GUID
        if (user == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "User not registered.", Details = $"User '{currentUserId}' is not registered in the system. Please contact system administrator." });
        }
        return Ok(user);
    }

    // --- Helper DTOs for Kiota Integration ---
    public class UserListRequest
    {
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string? Sort { get; set; }
        public string? Filter { get; set; }
    }
}