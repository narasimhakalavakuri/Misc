using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Repositories.Interfaces
{
    public interface IDepartmentRepository : IBaseRepository<Department>
    {
        Task<Department?> GetByDeptCodeAsync(string deptCode);
        Task<bool> HasUsersInDepartment(string deptCode);
        Task<bool> HasPositionReportsInDepartment(string deptCode);
    }
}