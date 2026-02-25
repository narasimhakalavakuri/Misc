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
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Generate a specific report.
    /// </summary>
    [HttpPost("{reportName}/generate")]
    [Authorize(Policy = "CanReport")] // Permission for reports
    [ProducesResponseType(typeof(FileContentResult), 200, MediaTypeNames.Application.Pdf)]
    [ProducesResponseType(typeof(ReportUrlResponse), 200, MediaTypeNames.Application.Json)]
    [ProducesResponseType(typeof(ErrorResponse), 400)]
    [ProducesResponseType(typeof(ErrorResponse), 401)]
    [ProducesResponseType(typeof(ErrorResponse), 403)]
    [ProducesResponseType(typeof(ErrorResponse), 404)]
    [ProducesResponseType(typeof(ErrorResponse), 422)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public async Task<IActionResult> GenerateReport(
        [FromRoute] string reportName,
        [FromBody] ReportGenerateRequest request,
        [FromHeader(Name = "X-Correlation-ID")] Guid? correlationId = null)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
        {
            return Unauthorized(new ErrorResponse { StatusCode = 401, Message = "User not identified.", Details = "Could not retrieve user ID from authentication token." });
        }

        var result = await _reportService.GenerateReportAsync(reportName, request, currentUserId);

        if (result.Item1 == null) // This indicates an error or not found
        {
            // The service layer should throw specific exceptions that get caught by the ProblemDetailsMiddleware
            // or return a structured error result. For simplicity, assuming service throws.
            return NotFound(new ErrorResponse { StatusCode = 404, Message = "Report not found or configuration invalid.", Details = $"Report '{reportName}' could not be generated." });
        }

        if (request.OutputFormat == "pdf")
        {
            return File(result.Item1, MediaTypeNames.Application.Pdf, $"{reportName}-{request.BusinessDate}.pdf");
        }
        else // Assuming JSON or URL for other formats
        {
            // For example, if it's an HTML report or a link to a stored report
            return Ok(new ReportUrlResponse { ReportUrl = result.Item2 });
        }
    }

    // Helper DTO for report URL response
    public record ReportUrlResponse(string ReportUrl);
}