using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Repositories.Interfaces
{
    public interface IAuditLogRepository : IBaseRepository<AuditLog>
    {
        Task LogEntryAsync(string action, string message, string userId);
    }
}