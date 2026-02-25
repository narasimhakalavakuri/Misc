using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Repositories.Interfaces;

public interface IDepartmentRepository
{
    Task<List<Department>> GetAllAsync(int? limit, int? offset, string? sort, string? filter, string? departmentCodePattern);
    Task<Department?> GetByCodeAsync(string departmentCode);
    Task AddAsync(Department department);
    Task UpdateAsync(Department department);
    Task DeleteAsync(string departmentCode);
}