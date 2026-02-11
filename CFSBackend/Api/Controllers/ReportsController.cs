using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectName.Application.Models.Reports;
using ProjectName.Application.Services.Interfaces;
using System.Security.Claims;

namespace ProjectName.Api.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    [Authorize(Policy = "CanReport")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        private string CurrentUserDepartment => User.FindFirst("Dept")?.Value ?? throw new UnauthorizedAccessException("Department not found in token.");

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<ReportDefinitionDto>))]
        public async Task<IActionResult> GetAvailableReports()
        {
            _logger.LogInformation("Fetching available reports.");
            var reports = await _reportService.GetAvailableReportsAsync(CurrentUserDepartment, User.FindFirst("AccessMask")?.Value ?? "");
            return Ok(reports);
        }

        [HttpPost("{reportName}/generate-pdf")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileContentResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GenerateReportPdf(string reportName, [FromBody] ReportRequestDto request)
        {
            _logger.LogInformation("Generating PDF report '{ReportName}' for department '{Dept}' and bizDate '{BizDate}'.", reportName, CurrentUserDepartment, request.BizDate.ToShortDateString());
            try
            {
                // In a real application, you'd likely pass a request DTO with report parameters
                // For now, using basic parameters from the Delphi app.
                var reportBytes = await _reportService.GenerateReportPdfAsync(reportName, CurrentUserDepartment, request);

                if (reportBytes == null || reportBytes.Length == 0)
                {
                    _logger.LogWarning("Report '{ReportName}' generation failed or returned empty content.", reportName);
                    return NotFound(new { message = $"Report '{reportName}' could not be generated or found." });
                }

                _logger.LogInformation("Report '{ReportName}' generated successfully.", reportName);
                return File(reportBytes, "application/pdf", $"{reportName}_{request.BizDate:yyyyMMdd}.pdf");
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Report definition for '{ReportName}' not found.", reportName);
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid arguments for report '{ReportName}'.", reportName);
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report '{ReportName}'.", reportName);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An internal server error occurred during report generation." });
            }
        }
    }
}