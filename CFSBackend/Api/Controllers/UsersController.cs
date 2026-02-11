using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Application.Models.Users;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Infrastructure.Exceptions;
using System.Security.Claims;

namespace ProjectName.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "CanUserAdmin")] // Only User Admins can manage users
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UsersController> _logger;

        public UsersController(IUserService userService, ILogger<UsersController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found in token.");

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<UserDto>))]
        public async Task<IActionResult> GetAllUsers()
        {
            _logger.LogInformation("Fetching all users by admin: {AdminId}", CurrentUserId);
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            _logger.LogInformation("Fetching user {Id} by admin: {AdminId}", id, CurrentUserId);
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User {Id} not found by admin {AdminId}.", id, CurrentUserId);
                return NotFound(new { message = $"User with ID {id} not found." });
            }
            return Ok(user);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            _logger.LogInformation("Creating new user {UserId} by admin: {AdminId}", request.UserId, CurrentUserId);
            try
            {
                var user = await _userService.CreateUserAsync(request, CurrentUserId);
                _logger.LogInformation("User {UserId} created by admin {AdminId}.", user.UserId, CurrentUserId);
                return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, user);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating user {UserId} by admin {AdminId}.", request.UserId, CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user {UserId} by admin {AdminId}.", request.UserId, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
        {
            _logger.LogInformation("Updating user {Id} by admin: {AdminId}", id, CurrentUserId);
            try
            {
                var user = await _userService.UpdateUserAsync(id, request, CurrentUserId);
                if (user == null)
                {
                    _logger.LogWarning("User {Id} not found for update by admin {AdminId}.", id, CurrentUserId);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }
                _logger.LogInformation("User {Id} updated by admin {AdminId}.", id, CurrentUserId);
                return Ok(user);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating user {Id} by admin {AdminId}.", id, CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {Id} by admin {AdminId}.", id, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            _logger.LogInformation("Deleting user {Id} by admin: {AdminId}", id, CurrentUserId);
            try
            {
                var success = await _userService.DeleteUserAsync(id, CurrentUserId);
                if (!success)
                {
                    _logger.LogWarning("User {Id} not found for deletion by admin {AdminId}.", id, CurrentUserId);
                    return NotFound(new { message = $"User with ID {id} not found." });
                }
                _logger.LogInformation("User {Id} deleted by admin {AdminId}.", id, CurrentUserId);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error deleting user {Id} by admin {AdminId}.", id, CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {Id} by admin {AdminId}.", id, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [Authorize] // Any logged in user can change their own password
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userIdFromToken = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdFromToken == null)
            {
                _logger.LogWarning("Change password attempt by unauthenticated user.");
                return Unauthorized(new { message = "User not authenticated." });
            }

            _logger.LogInformation("Change password attempt for user {UserId} by user: {UserIdFromToken}", request.UserId, userIdFromToken);

            try
            {
                await _userService.ChangePasswordAsync(request.UserId, request.OldPassword, request.NewPassword, userIdFromToken);
                _logger.LogInformation("Password changed successfully for user {UserId}.", request.UserId);
                return Ok(new { message = "Password changed successfully." });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "User {UserId} not found for password change.", request.UserId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized password change attempt for user {UserId}.", request.UserId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during password change for user {UserId}.", request.UserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}.", request.UserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }
    }
}