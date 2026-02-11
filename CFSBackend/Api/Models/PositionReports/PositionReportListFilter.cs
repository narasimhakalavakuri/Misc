using ProjectName.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.PositionReports
{
    public record PositionReportListFilter
    {
        public DateTime BusinessDate { get; init; } = DateTime.Today;
        public IEnumerable<PositionReportStatus>? Statuses { get; init; } // For filtering lists (e.g., U, M, K, E)
        public bool IncludeIncomplete { get; init; } = false; // For 'Awaiting Confirmation' tab
        public bool IsCorrectionList { get; init; } = false; // For 'Correction' tab
        public string? DepartmentCode { get; init; } // Optional: for 'All Sections' filter
    }
}