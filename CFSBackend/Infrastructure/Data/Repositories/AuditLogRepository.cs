using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;

namespace ProjectName.Infrastructure.Data.Repositories
{
    public class AuditLogRepository : BaseRepository<AuditLog>, IAuditLogRepository
    {
        private readonly ApplicationDbContext _context;
        public AuditLogRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task LogEntryAsync(string action, string message, string userId)
        {
            var logEntry = new AuditLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                LogStr = $"[{action}] {message}",
                LogTime = DateTime.UtcNow
            };
            await AddAsync(logEntry);
            await SaveAsync();
        }
    }
}