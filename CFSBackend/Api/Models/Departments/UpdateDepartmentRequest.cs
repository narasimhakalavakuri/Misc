using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.Departments
{
    public record UpdateDepartmentRequest(
        [Required] [MaxLength(100)] string DeptDesc,
        [Required] string ApprType
    );
}