using Microsoft.EntityFrameworkCore;
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;

namespace ProjectName.Infrastructure.Data.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByUserIdAsync(string userId)
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.ToUpper()); // Normalize to uppercase
        }
    }
}