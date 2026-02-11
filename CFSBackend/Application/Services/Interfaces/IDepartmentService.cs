using ProjectName.Application.Models.Departments;

namespace ProjectName.Application.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync();
        Task<DepartmentDto?> GetDepartmentByCodeAsync(string deptCode);
        Task<DepartmentDto> CreateDepartmentAsync(CreateDepartmentRequest request);
        Task<DepartmentDto?> UpdateDepartmentAsync(string deptCode, UpdateDepartmentRequest request);
        Task<bool> DeleteDepartmentAsync(string deptCode);
    }
}