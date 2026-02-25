using System.ComponentModel.DataAnnotations;

namespace PositionReporting.Infrastructure.Data.Entities;

// Anemic Domain Model - pure data structure
public class Department : BaseEntity
{
    [Key]
    [MaxLength(10)]
    public string DepartmentCode { get; set; } = string.Empty;
    [MaxLength(100)]
    public string DepartmentDescription { get; set; } = string.Empty;
    public DateTimeOffset? ClosedDate { get; set; }
    [MaxLength(100)]
    public string? ClosedBy { get; set; }
    [MaxLength(10)]
    public string Status { get; set; } = "OPEN"; // "OPEN" or "CLOSED"
}