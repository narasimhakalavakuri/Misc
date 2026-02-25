using Kiota.ApiClient.Models;
using PositionReporting.Api.Controllers; // For ReportUrlResponse

namespace PositionReporting.Api.Services.Interfaces;

public interface IReportService
{
    Task<(byte[]? reportContent, string? reportUrl)> GenerateReportAsync(string reportName, ReportGenerateRequest request, string currentUserId);
}