using Microsoft.EntityFrameworkCore;
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;

namespace ProjectName.Infrastructure.Data.Repositories
{
    public class DepartmentRepository : BaseRepository<Department>, IDepartmentRepository
    {
        private readonly ApplicationDbContext _context;

        public DepartmentRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Department?> GetByDeptCodeAsync(string deptCode)
        {
            return await _context.Departments
                .AsNoTracking() // Read-only operation
                .FirstOrDefaultAsync(d => d.DeptCode == deptCode.ToUpper()); // Normalize to uppercase
        }

        public async Task<bool> HasUsersInDepartment(string deptCode)
        {
            return await _context.Users.AnyAsync(u => u.Department == deptCode);
        }

        public async Task<bool> HasPositionReportsInDepartment(string deptCode)
        {
            return await _context.PositionReports.AnyAsync(pr => pr.Dept == deptCode);
        }
    }
}