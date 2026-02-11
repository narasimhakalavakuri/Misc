using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Application.Models.System;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Infrastructure.Exceptions;
using System.Security.Claims;

namespace ProjectName.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "CanSystemControl")]
    public class SystemController : ControllerBase
    {
        private readonly ISystemService _systemService;
        private readonly ILogger<SystemController> _logger;

        public SystemController(ISystemService systemService, ILogger<SystemController> logger)
        {
            _systemService = systemService;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found in token.");
        private string CurrentUserDepartment => User.FindFirst("Dept")?.Value ?? throw new UnauthorizedAccessException("Department not found in token.");

        [HttpGet("departments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DepartmentStatusDto>))]
        public async Task<IActionResult> GetDepartmentStatuses()
        {
            _logger.LogInformation("Fetching department statuses for System Control by user {UserId}", CurrentUserId);
            var statuses = await _systemService.GetAllDepartmentStatusesAsync();
            return Ok(statuses);
        }

        [HttpPost("close-department")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CloseDepartment([FromBody] CloseDepartmentRequest request)
        {
            _logger.LogInformation("Attempting to close department {DeptCode} for business date {VerifyDate} by user {UserId}", request.DeptCode, request.VerifyDate, CurrentUserId);
            try
            {
                await _systemService.CloseDepartmentAsync(request.DeptCode, request.VerifyDate, CurrentUserId);
                _logger.LogInformation("Department {DeptCode} closed successfully by user {UserId}", request.DeptCode, CurrentUserId);
                return Ok(new { message = $"Department {request.DeptCode} is now CLOSED." });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Department {DeptCode} not found during close attempt by {UserId}.", request.DeptCode, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error closing department {DeptCode} by {UserId}.", request.DeptCode, CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing department {DeptCode} by {UserId}.", request.DeptCode, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("open-department")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> OpenDepartment([FromBody] OpenDepartmentRequest request)
        {
            _logger.LogInformation("Attempting to open department {DeptCode} by user {UserId}", request.DeptCode, CurrentUserId);
            try
            {
                await _systemService.OpenDepartmentAsync(request.DeptCode, CurrentUserId);
                _logger.LogInformation("Department {DeptCode} opened successfully by user {UserId}", request.DeptCode, CurrentUserId);
                return Ok(new { message = $"Department {request.DeptCode} is now OPEN." });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Department {DeptCode} not found during open attempt by {UserId}.", request.DeptCode, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error opening department {DeptCode} by {UserId}.", request.DeptCode, CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening department {DeptCode} by {UserId}.", request.DeptCode, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet("department-status/{deptCode}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DepartmentStatusDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDepartmentCurrentStatus(string deptCode)
        {
            _logger.LogInformation("Fetching current department status for {DeptCode} by user {UserId}", deptCode, CurrentUserId);
            var status = await _systemService.GetDepartmentCurrentStatusAsync(deptCode);
            if (status == null)
            {
                return NotFound(new { message = $"Department {deptCode} not found." });
            }
            return Ok(status);
        }
    }
}