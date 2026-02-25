using Kiota.ApiClient.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PositionReporting.Api.Services.Interfaces;
using System.Net.Mime;

namespace PositionReporting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departmentService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
    {
        _departmentService = departmentService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve a list of all departments.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanQueryPositionEntries")] // Or a more general "CanViewDepartments" policy
    [ProducesResponseType(typeof(List<Department>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<Department>>> ListDepartments(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] string? sort,
        [FromQuery] string? filter,
        [FromQuery] string? departmentCodePattern,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var departments = await _departmentService.ListDepartmentsAsync(new DepartmentListRequest
        {
            Limit = limit,
            Offset = offset,
            Sort = sort,
            Filter = filter,
            DepartmentCodePattern = departmentCodePattern
        });
        return Ok(departments);
    }

    /// <summary>
    /// Create a new department.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanSysControl")] // System Administration permission
    [ProducesResponseType(typeof(Department), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<Department>> CreateDepartment(
        [FromBody] DepartmentCreateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var newDepartment = await _departmentService.CreateDepartmentAsync(request);
        return CreatedAtAction(nameof(GetDepartmentCloseStatus), new { departmentCode = newDepartment.DepartmentCode }, newDepartment);
    }

    /// <summary>
    /// Delete a department.
    /// </summary>
    [HttpDelete("{departmentCode}")]
    [Authorize(Policy = "CanSysControl")] // System Administration permission
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> DeleteDepartment(
        [FromRoute] string departmentCode,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        await _departmentService.DeleteDepartmentAsync(departmentCode);
        return NoContent();
    }

    /// <summary>
    /// Retrieve the closing status of a specific department.
    /// </summary>
    [HttpGet("{departmentCode}/close-status")]
    [Authorize(Policy = "CanQueryPositionEntries")] // Or CanSysControl
    [ProducesResponseType(typeof(DepartmentCloseStatus), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<DepartmentCloseStatus>> GetDepartmentCloseStatus(
        [FromRoute] string departmentCode,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var status = await _departmentService.GetDepartmentCloseStatusAsync(departmentCode);
        if (status == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Department not found.", Details = $"Department '{departmentCode}' does not exist." });
        }
        return Ok(status);
    }

    /// <summary>
    /// Update the closing status of a department.
    /// </summary>
    [HttpPatch("{departmentCode}/close-status")]
    [Authorize(Policy = "CanSysControl")] // System Control permission
    [ProducesResponseType(typeof(DepartmentCloseStatus), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<DepartmentCloseStatus>> UpdateDepartmentCloseStatus(
        [FromRoute] string departmentCode,
        [FromBody] DepartmentCloseStatusUpdateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var updatedStatus = await _departmentService.UpdateDepartmentCloseStatusAsync(departmentCode, request.IsClosed ?? false, request.VerifyDate, currentUserId);
        if (updatedStatus == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Department not found.", Details = $"Department '{departmentCode}' does not exist." });
        }
        return Ok(updatedStatus);
    }

    // --- Helper DTOs for Kiota Integration ---
    public class DepartmentListRequest
    {
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public string? DepartmentCodePattern { get; set; }
    }
}