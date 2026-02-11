using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Repositories.Interfaces
{
    public interface ICurrencyRepository : IBaseRepository<Currency>
    {
        Task<Currency?> GetByCodeAsync(string currCode);
    }
}