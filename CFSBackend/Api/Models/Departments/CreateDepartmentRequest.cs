using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.Departments
{
    public record CreateDepartmentRequest(
        [Required] [MaxLength(10)] string DeptCode, // MaxLength based on Delphi edtDEPT typically
        [Required] [MaxLength(100)] string DeptDesc,
        [Required] string ApprType // e.g., 'N' for normal
    );
}