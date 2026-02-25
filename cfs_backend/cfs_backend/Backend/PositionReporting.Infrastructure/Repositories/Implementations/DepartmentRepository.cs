using Microsoft.EntityFrameworkCore;
using PositionReporting.Infrastructure.Data;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;

namespace PositionReporting.Infrastructure.Repositories.Implementations;

public class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DepartmentRepository> _logger;

    public DepartmentRepository(ApplicationDbContext context, ILogger<DepartmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<Department>> GetAllAsync(int? limit, int? offset, string? sort, string? filter, string? departmentCodePattern)
    {
        _logger.LogDebug("Retrieving all departments with limit: {Limit}, offset: {Offset}, sort: {Sort}, filter: {Filter}, pattern: {Pattern}", limit, offset, sort, filter, departmentCodePattern);
        var query = _context.Departments.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(departmentCodePattern))
        {
            // Replace '%' with SQL-compatible '%' if not already for LIKE operations
            var sqlPattern = departmentCodePattern.Replace("*", "%");
            query = query.Where(d => EF.Functions.Like(d.DepartmentCode, sqlPattern));
        }

        if (!string.IsNullOrWhiteSpace(filter))
        {
            // Simplified filtering logic
            query = query.Where(d => d.DepartmentCode.Contains(filter) || d.DepartmentDescription.Contains(filter));
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(':');
            if (parts.Length == 2)
            {
                query = parts[0].ToLowerInvariant() switch
                {
                    "departmentcode" => parts[1].ToLowerInvariant() == "desc" ? query.OrderByDescending(d => d.DepartmentCode) : query.OrderBy(d => d.DepartmentCode),
                    "departmentdescription" => parts[1].ToLowerInvariant() == "desc" ? query.OrderByDescending(d => d.DepartmentDescription) : query.OrderBy(d => d.DepartmentDescription),
                    _ => query
                };
            }
        }
        else
        {
            query = query.OrderBy(d => d.DepartmentCode);
        }

        if (offset.HasValue)
        {
            query = query.Skip(offset.Value);
        }
        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<Department?> GetByCodeAsync(string departmentCode)
    {
        _logger.LogDebug("Retrieving department by code: {DepartmentCode}", departmentCode);
        return await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.DepartmentCode == departmentCode);
    }

    public async Task AddAsync(Department department)
    {
        _logger.LogDebug("Adding department: {DepartmentCode}", department.DepartmentCode);
        await _context.Departments.AddAsync(department);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Department department)
    {
        _logger.LogDebug("Updating department: {DepartmentCode}", department.DepartmentCode);
        _context.Departments.Update(department);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string departmentCode)
    {
        _logger.LogDebug("Deleting department: {DepartmentCode}", departmentCode);
        var department = await _context.Departments.FindAsync(departmentCode);
        if (department != null)
        {
            _context.Departments.Remove(department);
            await _context.SaveChangesAsync();
        }
    }
}