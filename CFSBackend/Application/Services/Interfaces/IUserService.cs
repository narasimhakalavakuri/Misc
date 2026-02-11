using ProjectName.Application.Models.Users;

namespace ProjectName.Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(Guid id);
        Task<UserDto> CreateInitialAdminUserAsync(string userId, string password, string accessMask); // For initial setup
        Task<UserDto> CreateUserAsync(CreateUserRequest request, string creatorUserId);
        Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, string updaterUserId);
        Task<bool> DeleteUserAsync(Guid id, string deleterUserId);
        Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, string changerUserId);
        Task SeedDefaultDepartments();
        Task SeedDefaultCurrencies();
    }
}