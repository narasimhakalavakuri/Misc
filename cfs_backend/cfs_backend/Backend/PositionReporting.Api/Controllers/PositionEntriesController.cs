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
public class PositionEntriesController : ControllerBase
{
    private readonly IPositionEntryService _positionEntryService;
    private readonly ILogger<PositionEntriesController> _logger;

    public PositionEntriesController(IPositionEntryService positionEntryService, ILogger<PositionEntriesController> logger)
    {
        _positionEntryService = positionEntryService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieve a paginated list of position entries.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "CanQueryPositionEntries")]
    [ProducesResponseType(typeof(List<PositionEntrySummary>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<PositionEntrySummary>>> ListPositionEntries(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] string? sort,
        [FromQuery] string? filter,
        [FromQuery] string? departmentId,
        [FromQuery] Microsoft.Kiota.Abstractions.Date? businessDate,
        [FromQuery] PositionEntry_status[]? status,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        // Layer 1: Controller responsibility - DTO mapping and returning ActionResult. No business logic here.
        var request = new PositionEntryListRequest
        {
            Limit = limit,
            Offset = offset,
            Sort = sort,
            Filter = filter,
            DepartmentId = departmentId,
            BusinessDate = businessDate,
            Status = status?.Select(s => s.ToString()).ToList() // Convert enum array to string list
        };

        var entries = await _positionEntryService.ListPositionEntriesAsync(request);
        return Ok(entries);
    }

    /// <summary>
    /// Create a new financial position entry.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanInputPositionEntries")]
    [ProducesResponseType(typeof(PositionEntry), 201)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PositionEntry>> CreatePositionEntry(
        [FromBody] PositionEntryCreateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var newEntry = await _positionEntryService.CreatePositionEntryAsync(request, currentUserId);
        return CreatedAtAction(nameof(GetPositionEntryById), new { positionEntryId = newEntry.Id }, newEntry);
    }

    /// <summary>
    /// Retrieve a specific position entry by ID.
    /// </summary>
    [HttpGet("{positionEntryId}")]
    [Authorize(Policy = "CanQueryPositionEntries")]
    [ProducesResponseType(typeof(PositionEntry), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PositionEntry>> GetPositionEntryById(
        [FromRoute] string positionEntryId,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var entry = await _positionEntryService.GetPositionEntryByIdAsync(positionEntryId, currentUserId);
        if (entry == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Position entry not found.", Details = $"Position entry '{positionEntryId}' does not exist." });
        }
        return Ok(entry);
    }

    /// <summary>
    /// Update an existing financial position entry.
    /// </summary>
    [HttpPatch("{positionEntryId}")]
    [Authorize(Policy = "CanInputPositionEntries")]
    [ProducesResponseType(typeof(PositionEntry), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PositionEntry>> UpdatePositionEntry(
        [FromRoute] string positionEntryId,
        [FromBody] PositionEntryUpdateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var updatedEntry = await _positionEntryService.UpdatePositionEntryAsync(positionEntryId, request, currentUserId);
        if (updatedEntry == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Position entry not found.", Details = $"Position entry '{positionEntryId}' does not exist." });
        }
        return Ok(updatedEntry);
    }

    /// <summary>
    /// Logically delete a position entry.
    /// </summary>
    [HttpDelete("{positionEntryId}")]
    [Authorize(Policy = "CanInputPositionEntries")] // Or a specific delete policy if applicable
    [ProducesResponseType(204)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> DeletePositionEntry(
        [FromRoute] string positionEntryId,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        await _positionEntryService.DeletePositionEntryAsync(positionEntryId, currentUserId);
        return NoContent(); // 204 No Content
    }

    /// <summary>
    /// Retrieve a list of position entries awaiting confirmation.
    /// </summary>
    [HttpGet("awaiting-confirmation")]
    [Authorize(Policy = "CanQueryPositionEntries")]
    [ProducesResponseType(typeof(List<PositionEntrySummary>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<PositionEntrySummary>>> ListAwaitingConfirmationEntries(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] string? sort,
        [FromQuery] string? filter,
        [FromQuery] string departmentId, // Required
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var request = new PositionEntryListRequest
        {
            Limit = limit,
            Offset = offset,
            Sort = sort,
            Filter = filter,
            DepartmentId = departmentId,
            Status = new List<string> { PositionEntry_status.M.ToString() } // Only 'M' for awaiting confirmation
        };
        var entries = await _positionEntryService.ListAwaitingConfirmationEntriesAsync(request);
        return Ok(entries);
    }

    /// <summary>
    /// Retrieve a list of position entries eligible for correction.
    /// </summary>
    [HttpGet("correction-candidates")]
    [Authorize(Policy = "CanCheckPositionEntries")] // Or a specific correction policy
    [ProducesResponseType(typeof(List<PositionEntrySummary>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<PositionEntrySummary>>> ListCorrectionCandidates(
        [FromQuery] int? limit,
        [FromQuery] int? offset,
        [FromQuery] string? sort,
        [FromQuery] string? filter,
        [FromQuery] string departmentId, // Required
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var request = new PositionEntryListRequest
        {
            Limit = limit,
            Offset = offset,
            Sort = sort,
            Filter = filter,
            DepartmentId = departmentId,
            Status = new List<string> { PositionEntry_status.E.ToString() } // Only 'E' for correction candidates
        };
        var entries = await _positionEntryService.ListCorrectionCandidatesAsync(request);
        return Ok(entries);
    }

    /// <summary>
    /// Check for potential duplicate position entries.
    /// </summary>
    [HttpPost("check-duplicates")]
    [Authorize(Policy = "CanInputPositionEntries")]
    [ProducesResponseType(typeof(List<DuplicateCheckResponse>), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<List<DuplicateCheckResponse>>> CheckPositionEntryDuplicates(
        [FromBody] DuplicateCheckRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var duplicates = await _positionEntryService.CheckPositionEntryDuplicatesAsync(request);
        if (duplicates != null && duplicates.Any())
        {
            // OpenAPI spec indicates 409 for duplicates, with a body
            return Conflict(duplicates);
        }
        return Ok(new List<DuplicateCheckResponse>()); // Return empty array if no duplicates
    }

    /// <summary>
    /// Update the status of a specific position entry.
    /// </summary>
    [HttpPatch("{positionEntryId}/status")]
    [Authorize(Policy = "CanCheckPositionEntries")]
    [ProducesResponseType(typeof(PositionEntry), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PositionEntry>> UpdatePositionEntryStatus(
        [FromRoute] string positionEntryId,
        [FromBody] PositionEntryStatusUpdateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var updatedEntry = await _positionEntryService.UpdatePositionEntryStatusAsync(positionEntryId, request.Status.ToString(), currentUserId);
        if (updatedEntry == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Position entry not found.", Details = $"Position entry '{positionEntryId}' does not exist." });
        }
        return Ok(updatedEntry);
    }

    /// <summary>
    /// Check out a position entry for exclusive editing.
    /// </summary>
    [HttpPatch("{positionEntryId}/checkout")]
    [Authorize(Policy = "CanInputPositionEntries")] // Or specific checkout policy
    [ProducesResponseType(typeof(PositionEntry), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 409)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PositionEntry>> CheckoutPositionEntry(
        [FromRoute] string positionEntryId,
        [FromBody] PositionEntryCheckoutRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var updatedEntry = await _positionEntryService.CheckoutPositionEntryAsync(positionEntryId, request.Action.ToString(), currentUserId);
        if (updatedEntry == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Position entry not found.", Details = $"Position entry '{positionEntryId}' does not exist." });
        }
        return Ok(updatedEntry);
    }

    /// <summary>
    /// Force-unlock a checked-out position entry.
    /// </summary>
    [HttpPatch("{positionEntryId}/unlock")]
    [Authorize(Policy = "CanSysControl")] // Typically admin/syscontrol permission
    [ProducesResponseType(typeof(PositionEntry), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<ActionResult<PositionEntry>> UnlockPositionEntry(
        [FromRoute] string positionEntryId,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var updatedEntry = await _positionEntryService.UnlockPositionEntryAsync(positionEntryId);
        if (updatedEntry == null)
        {
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Position entry not found.", Details = $"Position entry '{positionEntryId}' does not exist." });
        }
        return Ok(updatedEntry);
    }

    // --- Helper DTOs for Kiota Integration ---
    // These are simplified internal DTOs used by the service layer, 
    // mapping to/from Kiota.ApiClient.Models is assumed to happen within the service.

    public class PositionEntryListRequest
    {
        public int? Limit { get; set; }
        public int? Offset { get; set; }
        public string? Sort { get; set; }
        public string? Filter { get; set; }
        public string? DepartmentId { get; set; }
        public Microsoft.Kiota.Abstractions.Date? BusinessDate { get; set; }
        public List<string>? Status { get; set; }
    }
}