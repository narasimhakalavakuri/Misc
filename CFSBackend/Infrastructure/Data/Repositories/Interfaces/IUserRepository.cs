using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Repositories.Interfaces
{
    public interface IUserRepository : IBaseRepository<User>
    {
        Task<User?> GetUserByUserIdAsync(string userId);
    }
}