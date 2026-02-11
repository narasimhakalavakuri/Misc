using ProjectName.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.PositionReports
{
    public record UpdatePositionReportStatusRequest(
        [Required] Guid[] Uids,
        [Required] PositionReportStatus Status
    );
}