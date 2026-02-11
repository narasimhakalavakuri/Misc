using ProjectName.Application.Models.Auth;

namespace ProjectName.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> LoginAsync(LoginRequest request);
    }
}