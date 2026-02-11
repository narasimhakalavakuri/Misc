using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Application.Models.Departments;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Infrastructure.Exceptions;

namespace ProjectName.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "CanUserAdmin")] // Only User Admins can manage departments
    public class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;
        private readonly ILogger<DepartmentsController> _logger;

        public DepartmentsController(IDepartmentService departmentService, ILogger<DepartmentsController> logger)
        {
            _departmentService = departmentService;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DepartmentDto>))]
        public async Task<IActionResult> GetAllDepartments()
        {
            _logger.LogInformation("Fetching all departments.");
            var departments = await _departmentService.GetAllDepartmentsAsync();
            return Ok(departments);
        }

        [HttpGet("{code}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DepartmentDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDepartmentByCode(string code)
        {
            _logger.LogInformation("Fetching department with code: {Code}", code);
            var department = await _departmentService.GetDepartmentByCodeAsync(code);
            if (department == null)
            {
                _logger.LogWarning("Department with code {Code} not found.", code);
                return NotFound(new { message = $"Department with code {code} not found." });
            }
            return Ok(department);
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(DepartmentDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDepartment([FromBody] CreateDepartmentRequest request)
        {
            _logger.LogInformation("Creating new department: {DeptCode}", request.DeptCode);
            try
            {
                var department = await _departmentService.CreateDepartmentAsync(request);
                _logger.LogInformation("Department {DeptCode} created successfully.", department.DeptCode);
                return CreatedAtAction(nameof(GetDepartmentByCode), new { code = department.DeptCode }, department);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating department: {DeptCode}", request.DeptCode);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating department: {DeptCode}", request.DeptCode);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpPut("{code}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DepartmentDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateDepartment(string code, [FromBody] UpdateDepartmentRequest request)
        {
            _logger.LogInformation("Updating department with code: {Code}", code);
            try
            {
                var department = await _departmentService.UpdateDepartmentAsync(code, request);
                if (department == null)
                {
                    _logger.LogWarning("Department with code {Code} not found for update.", code);
                    return NotFound(new { message = $"Department with code {code} not found." });
                }
                _logger.LogInformation("Department {Code} updated successfully.", code);
                return Ok(department);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating department: {Code}", code);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating department: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpDelete("{code}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteDepartment(string code)
        {
            _logger.LogInformation("Deleting department with code: {Code}", code);
            try
            {
                var success = await _departmentService.DeleteDepartmentAsync(code);
                if (!success)
                {
                    _logger.LogWarning("Department with code {Code} not found for deletion.", code);
                    return NotFound(new { message = $"Department with code {code} not found." });
                }
                _logger.LogInformation("Department {Code} deleted successfully.", code);
                return NoContent();
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error deleting department: {Code}", code);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting department: {Code}", code);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }
    }
}