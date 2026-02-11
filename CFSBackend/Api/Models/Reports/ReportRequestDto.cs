using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.Reports
{
    public record ReportRequestDto(
        [Required] DateTime BizDate
        // Add more parameters as needed for specific reports
    );
}