using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Repositories.Interfaces;

public interface IUserRepository
{
    Task<List<User>> GetAllAsync(int? limit, int? offset, string? sort, string? filter);
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByExternalIdAsync(string externalUserId);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(Guid id);
}