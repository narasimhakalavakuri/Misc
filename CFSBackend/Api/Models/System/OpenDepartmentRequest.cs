using System.ComponentModel.DataAnnotations;

namespace ProjectName.Application.Models.System
{
    public record OpenDepartmentRequest(
        [Required] string DeptCode
    );
}