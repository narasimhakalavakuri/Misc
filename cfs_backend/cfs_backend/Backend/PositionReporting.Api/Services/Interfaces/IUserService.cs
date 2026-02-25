using System.Security.Claims;
using PositionReporting.Core.Utils; // For Result pattern
using Kiota.ApiClient.Models; // For Kiota generated models

namespace PositionReporting.Api.Services.Interfaces;

public interface IUserService
{
    Task<Result<Infrastructure.Data.Entities.User, string>> ValidateSamlUserAsync(string externalUserId, IEnumerable<Claim> samlClaims);
    Task<List<Kiota.ApiClient.Models.User>> ListUsersAsync(Controllers.UsersController.UserListRequest request);
    Task<Kiota.ApiClient.Models.User?> GetUserByIdAsync(Guid id);
    Task<Kiota.ApiClient.Models.User> CreateUserAsync(UserCreateRequest request);
    Task<Kiota.ApiClient.Models.User?> UpdateUserAsync(string userId, UserUpdateRequest request);
    Task DeleteUserAsync(string userId);
}