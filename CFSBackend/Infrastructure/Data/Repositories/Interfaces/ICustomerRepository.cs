using ProjectName.Infrastructure.Data.Entities;

namespace ProjectName.Infrastructure.Data.Repositories.Interfaces
{
    public interface ICustomerRepository : IBaseRepository<Customer>
    {
        Task<Customer?> GetCustomerByAccountNumberOrAbbrvAsync(string identifier);
    }
}