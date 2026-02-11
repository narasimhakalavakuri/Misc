using ProjectName.Application.Models.System;

namespace ProjectName.Application.Services.Interfaces
{
    public interface ISystemService
    {
        Task<IEnumerable<DepartmentStatusDto>> GetAllDepartmentStatusesAsync();
        Task<DepartmentStatusDto?> GetDepartmentCurrentStatusAsync(string deptCode);
        Task CloseDepartmentAsync(string deptCode, DateTime verifyDate, string currentUserId);
        Task OpenDepartmentAsync(string deptCode, string currentUserId);
    }
}