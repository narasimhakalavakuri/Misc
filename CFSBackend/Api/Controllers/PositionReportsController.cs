using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Application.Models.PositionReports;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Infrastructure.Exceptions;
using System.Security.Claims;

namespace ProjectName.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize]
    public class PositionReportsController : ControllerBase
    {
        private readonly IPositionReportService _positionReportService;
        private readonly ILogger<PositionReportsController> _logger;

        public PositionReportsController(IPositionReportService positionReportService, ILogger<PositionReportsController> logger)
        {
            _positionReportService = positionReportService;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? throw new UnauthorizedAccessException("User ID not found in token.");
        private string CurrentUserDepartment => User.FindFirst("Dept")?.Value ?? throw new UnauthorizedAccessException("Department not found in token.");
        private string CurrentUserAccessMask => User.FindFirst("AccessMask")?.Value ?? "";

        [HttpPost]
        [Authorize(Policy = "CanInput")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PositionReportDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreatePositionReport([FromBody] CreatePositionReportRequest request)
        {
            _logger.LogInformation("Creating new position report by user: {UserId}", CurrentUserId);
            try
            {
                var positionReport = await _positionReportService.CreatePositionReportAsync(request, CurrentUserDepartment, CurrentUserId);
                _logger.LogInformation("Position report {Uid} created by {UserId}.", positionReport.Uid, CurrentUserId);
                return CreatedAtAction(nameof(GetPositionReportById), new { id = positionReport.Uid }, positionReport);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error creating position report by {UserId}.", CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating position report by {UserId}.", CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Policy = "CanInput")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PositionReportDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdatePositionReport(Guid id, [FromBody] UpdatePositionReportRequest request)
        {
            _logger.LogInformation("Updating position report {Id} by user: {UserId}", id, CurrentUserId);
            try
            {
                var positionReport = await _positionReportService.UpdatePositionReportAsync(id, request, CurrentUserDepartment, CurrentUserId);
                _logger.LogInformation("Position report {Id} updated by {UserId}.", id, CurrentUserId);
                return Ok(positionReport);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Position report {Id} not found for update by {UserId}.", id, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating position report {Id} by {UserId}.", id, CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating position report {Id} by {UserId}.", id, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet("{id}")]
        [Authorize(Policy = "CanInput")] // Input users need to retrieve corrections
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PositionReportDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPositionReportById(Guid id)
        {
            _logger.LogInformation("Fetching position report {Id} by user: {UserId}", id, CurrentUserId);
            var positionReport = await _positionReportService.GetPositionReportByIdAsync(id, CurrentUserDepartment, CurrentUserId);
            if (positionReport == null)
            {
                _logger.LogWarning("Position report {Id} not found for user {UserId}.", id, CurrentUserId);
                return NotFound();
            }
            return Ok(positionReport);
        }


        // Used for ApprovalList, Correction, Incomplete lists
        [HttpGet("list")]
        [Authorize(Policy = "CanCheck")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PositionReportListItemDto>))]
        public async Task<IActionResult> GetPositionReportsForListings([FromQuery] PositionReportListFilter filter)
        {
            _logger.LogInformation("Fetching position reports for listing with filter: {Filter} by user {UserId}", filter, CurrentUserId);
            var reports = await _positionReportService.GetPositionReportsForListingAsync(filter, CurrentUserDepartment, CurrentUserId);
            return Ok(reports);
        }

        [HttpPost("status")]
        [Authorize(Policy = "CanCheck")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdatePositionReportStatus([FromBody] UpdatePositionReportStatusRequest request)
        {
            _logger.LogInformation("Updating status for position reports by user: {UserId}", CurrentUserId);
            try
            {
                var success = await _positionReportService.UpdatePositionReportStatusAsync(request, CurrentUserDepartment, CurrentUserId);
                if (!success)
                {
                    _logger.LogWarning("Failed to update status for some position reports by user: {UserId}", CurrentUserId);
                    return BadRequest(new { message = "Failed to update status for some items. Check UIDs and permissions." });
                }
                _logger.LogInformation("Status updated for position reports by user: {UserId}", CurrentUserId);
                return Ok(new { message = "Position report(s) status updated successfully." });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error updating status for position reports by {UserId}.", CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Some position reports not found during status update by {UserId}.", CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating status for position reports by {UserId}.", CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }


        [HttpPost("checkout")]
        [Authorize(Policy = "CanCheck")] // Checkers can checkout for correction/approval
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckoutPositionReports([FromBody] Guid[] uids)
        {
            _logger.LogInformation("Checking out position reports by user: {UserId}", CurrentUserId);
            try
            {
                await _positionReportService.CheckoutPositionReportsAsync(uids, CurrentUserDepartment, CurrentUserId);
                _logger.LogInformation("Position reports checked out by {UserId}.", CurrentUserId);
                return Ok(new { message = "Position report(s) checked out successfully." });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during checkout of position reports by {UserId}.", CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking out position reports by {UserId}.", CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("checkin")]
        [Authorize(Policy = "CanCheck")] // Checkers can checkin for correction/approval
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckinPositionReports([FromBody] Guid[] uids)
        {
            _logger.LogInformation("Checking in position reports by user: {UserId}", CurrentUserId);
            try
            {
                await _positionReportService.CheckinPositionReportsAsync(uids, CurrentUserDepartment, CurrentUserId);
                _logger.LogInformation("Position reports checked in by {UserId}.", CurrentUserId);
                return Ok(new { message = "Position report(s) checked in successfully." });
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during check-in of position reports by {UserId}.", CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking in position reports by {UserId}.", CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Policy = "CanCheck")] // Only checkers can logically delete
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePositionReport(Guid id)
        {
            _logger.LogInformation("Logical deletion of position report {Id} by user: {UserId}", id, CurrentUserId);
            try
            {
                await _positionReportService.LogicallyDeletePositionReportAsync(id, CurrentUserDepartment, CurrentUserId);
                _logger.LogInformation("Position report {Id} logically deleted by {UserId}.", id, CurrentUserId);
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "Position report {Id} not found for logical deletion by {UserId}.", id, CurrentUserId);
                return NotFound(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to delete position report {Id} by {UserId}.", id, CurrentUserId);
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logically deleting position report {Id} by {UserId}.", id, CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpPost("duplicate-check")]
        [Authorize(Policy = "CanInput")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<DuplicatePositionReportDto>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CheckForDuplicates([FromBody] DuplicateCheckRequest request)
        {
            _logger.LogInformation("Checking for duplicates for position report by user: {UserId}", CurrentUserId);
            try
            {
                var duplicates = await _positionReportService.CheckForDuplicatesAsync(request, CurrentUserDepartment, CurrentUserId);
                return Ok(duplicates);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation error during duplicate check by {UserId}.", CurrentUserId);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicates by {UserId}.", CurrentUserId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred." });
            }
        }

        [HttpGet("nostro-accounts/{currencyCode}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> GetNostroAccounts(string currencyCode)
        {
            _logger.LogInformation("Fetching Nostro accounts for currency {CurrencyCode} by user {UserId}", currencyCode, CurrentUserId);
            var nostroAccounts = await _positionReportService.GetNostroAccountsAsync(currencyCode);
            return Ok(nostroAccounts);
        }

        [HttpGet("currencies/{currencyCode}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CurrencyDetailsDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrencyDetails(string currencyCode)
        {
            _logger.LogInformation("Fetching currency details for {CurrencyCode} by user {UserId}", currencyCode, CurrentUserId);
            var currency = await _positionReportService.GetCurrencyDetailsAsync(currencyCode);
            if (currency == null)
            {
                return NotFound(new { message = $"Currency {currencyCode} not found." });
            }
            return Ok(currency);
        }

        [HttpGet("currency-for-account/{accountNumber}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCurrencyForAccount(string accountNumber)
        {
            _logger.LogInformation("Fetching currency for account {AccountNumber} by user {UserId}", accountNumber, CurrentUserId);
            var currency = await _positionReportService.GetCurrencyForAccountAsync(accountNumber);
            if (currency == null)
            {
                return NotFound(new { message = $"Currency for account {accountNumber} not found." });
            }
            return Ok(currency);
        }
    }
}