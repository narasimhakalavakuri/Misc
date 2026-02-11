using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.System
{
    public record CloseDepartmentRequest(
        [Required] string DeptCode,
        [Required] DateTime VerifyDate
    );
}