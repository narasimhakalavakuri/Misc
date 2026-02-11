using Microsoft.Extensions.Configuration;
using ProjectName.Application.Models.Reports;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Domain.Models;
using ProjectName.Infrastructure.Auth; // Assuming JwtSettings is here
using ProjectName.Infrastructure.Exceptions;
using ProjectName.Infrastructure.Data.Repositories.Interfaces; // For audit logging
using System.Text;

namespace ProjectName.Application.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ReportService> _logger;
        private readonly IJwtService _jwtService; // Used to get credentials if needed for reporting (Delphi's .prm logic)
        private readonly IAuditLogRepository _auditLogRepository; // For logging report generation

        private readonly string _reportPath;

        public ReportService(IConfiguration configuration, ILogger<ReportService> logger, IJwtService jwtService, IAuditLogRepository auditLogRepository)
        {
            _configuration = configuration;
            _logger = logger;
            _jwtService = jwtService;
            _auditLogRepository = auditLogRepository;

            _reportPath = _configuration.GetValue<string>("AppSettings:ReportPath") ?? "Reports/";
            if (!Directory.Exists(_reportPath))
            {
                Directory.CreateDirectory(_reportPath);
            }
        }

        public async Task<IEnumerable<ReportDefinitionDto>> GetAvailableReportsAsync(string userDepartment, string userAccessMask)
        {
            _logger.LogInformation("Fetching available reports for department {Dept} and access mask {AccessMask}.", userDepartment, userAccessMask);
            var reports = new List<ReportDefinitionDto>();
            var reportConfigs = _configuration.GetSection("Reports").GetChildren();

            foreach (var config in reportConfigs)
            {
                var name = config.Key;
                var fileName = config["FileName"];
                var permissions = config["Permissions"]; // e.g., "0110000" for ACC_INPUT, ACC_CHECK
                var description = config["Description"];

                // Check if the user has the required permissions for the report
                if (!string.IsNullOrEmpty(permissions) && !string.IsNullOrEmpty(userAccessMask))
                {
                    bool hasPermission = false;
                    for (int i = 0; i < permissions.Length && i < userAccessMask.Length; i++)
                    {
                        if (permissions[i] == '1' && userAccessMask[i] == '1')
                        {
                            hasPermission = true;
                            break;
                        }
                    }
                    if (!hasPermission)
                    {
                        _logger.LogDebug("User does not have permission for report '{ReportName}'. Required: {Permissions}, User: {AccessMask}", name, permissions, userAccessMask);
                        continue;
                    }
                }
                reports.Add(new ReportDefinitionDto(name, description ?? "", fileName ?? ""));
            }
            return await Task.FromResult(reports);
        }


        public async Task<byte[]> GenerateReportPdfAsync(string reportName, string userDepartment, ReportRequestDto request)
        {
            _logger.LogInformation("Attempting to generate PDF report '{ReportName}' for department '{Dept}' and business date '{BizDate}'.", reportName, userDepartment, request.BizDate.ToShortDateString());

            var reportConfig = _configuration.GetSection($"Reports:{reportName}");
            var fileName = reportConfig["FileName"];
            var reportDefinition = reportConfig["Definition"]; // Path to the report template if using a templating engine (e.g., FastReport, QuestPDF)

            if (string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Report definition for '{ReportName}' not found.", reportName);
                throw new FileNotFoundException($"Report definition for '{reportName}' not found.");
            }

            // In a real application, this is where you'd integrate with a reporting library (e.g., FastReport.Core, QuestPDF).
            // For this example, we'll simulate PDF generation.
            // A more sophisticated implementation would:
            // 1. Load report template (e.g., RDL, FastReport file, QuestPDF code).
            // 2. Fetch data from the database based on report parameters (BizDate, Dept, etc.).
            // 3. Populate the template with data.
            // 4. Export to PDF.

            // Simulate PDF content (e.g., using a simple text string for now)
            string reportContent = $"--- Cash Flow System Report: {reportName} ---\n\n" +
                                   $"Department: {userDepartment}\n" +
                                   $"Business Date: {request.BizDate:yyyy-MM-dd}\n" +
                                   $"Generated On: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n" +
                                   $"This is a placeholder report content. In a real system, data would be dynamically fetched and rendered.\n\n" +
                                   $"Report details from configuration: {reportConfig["Description"]}\n";

            // Example of how to integrate with a PDF library if you were using one:
            // byte[] pdfBytes = await SomePdfReportingLibrary.GeneratePdf(templatePath, data, request.Parameters);

            // For now, just convert the string to bytes. This will generate a text file,
            // but the Content-Type header will make the browser treat it as PDF.
            // For actual PDF, use a library.
            byte[] pdfBytes = Encoding.UTF8.GetBytes(reportContent);

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionGenerateReport, $"Report '{reportName}' generated for department {userDepartment} on {request.BizDate:yyyy-MM-dd}.", "SYSTEM");

            _logger.LogInformation("Successfully simulated PDF generation for report '{ReportName}'.", reportName);
            return pdfBytes;
        }
    }
}