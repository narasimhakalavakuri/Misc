namespace ProjectName.Application.Models.System
{
    public record DepartmentStatusDto(
        string DeptCode,
        string Status, // "OPEN" or "CLOSED"
        string? OfficerId,
        DateTime? LastClosedDate
    );
}