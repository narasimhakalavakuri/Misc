using ProjectName.Application.Models.Reports;

namespace ProjectName.Application.Services.Interfaces
{
    public interface IReportService
    {
        Task<IEnumerable<ReportDefinitionDto>> GetAvailableReportsAsync(string userDepartment, string userAccessMask);
        Task<byte[]> GenerateReportPdfAsync(string reportName, string userDepartment, ReportRequestDto request);
    }
}