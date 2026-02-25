using Microsoft.EntityFrameworkCore;
using PositionReporting.Infrastructure.Data;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;

namespace PositionReporting.Infrastructure.Repositories.Implementations;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(ApplicationDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<User>> GetAllAsync(int? limit, int? offset, string? sort, string? filter)
    {
        _logger.LogDebug("Retrieving all users with limit: {Limit}, offset: {Offset}, sort: {Sort}, filter: {Filter}", limit, offset, sort, filter);
        var query = _context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(u => u.UserId.Contains(filter) || (u.Email != null && u.Email.Contains(filter)) || (u.DepartmentId != null && u.DepartmentId.Contains(filter)));
        }

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(':');
            if (parts.Length == 2)
            {
                query = parts[0].ToLowerInvariant() switch
                {
                    "userid" => parts[1].ToLowerInvariant() == "desc" ? query.OrderByDescending(u => u.UserId) : query.OrderBy(u => u.UserId),
                    "departmentid" => parts[1].ToLowerInvariant() == "desc" ? query.OrderByDescending(u => u.DepartmentId) : query.OrderBy(u => u.DepartmentId),
                    _ => query
                };
            }
        }
        else
        {
            query = query.OrderBy(u => u.UserId);
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

    public async Task<User?> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Retrieving user by ID: {Id}", id);
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByExternalIdAsync(string externalUserId)
    {
        _logger.LogDebug("Retrieving user by external ID: {ExternalUserId}", externalUserId);
        return await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == externalUserId);
    }

    public async Task AddAsync(User user)
    {
        _logger.LogDebug("Adding new user: {UserId}", user.UserId);
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        _logger.LogDebug("Updating user: {UserId}", user.UserId);
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogDebug("Deleting user by ID: {Id}", id);
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}