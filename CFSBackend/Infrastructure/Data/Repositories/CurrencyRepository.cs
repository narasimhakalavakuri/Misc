using Microsoft.EntityFrameworkCore;
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;

namespace ProjectName.Infrastructure.Data.Repositories
{
    public class CurrencyRepository : BaseRepository<Currency>, ICurrencyRepository
    {
        private readonly ApplicationDbContext _context;

        public CurrencyRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<Currency?> GetByCodeAsync(string currCode)
        {
            return await _context.Currencies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CurrCode == currCode.ToUpper());
        }
    }
}