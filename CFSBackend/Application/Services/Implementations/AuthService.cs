using ProjectName.Application.Models.Auth;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Infrastructure.Auth;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;
using ProjectName.Infrastructure.Exceptions;
using ProjectName.Domain.Models;

namespace ProjectName.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;
        private readonly JwtSettings _jwtSettings;

        public AuthService(IUserRepository userRepository, IJwtService jwtService, IPasswordHasher passwordHasher, ILogger<AuthService> logger, JwtSettings jwtSettings)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _passwordHasher = passwordHasher;
            _logger = logger;
            _jwtSettings = jwtSettings;
        }

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            _logger.LogInformation("Attempting to log in user: {UserId}", request.UserId);

            if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new ValidationException("User ID and password cannot be empty.");
            }

            // Normalize UserId to uppercase as per Delphi app
            var normalizedUserId = request.UserId.ToUpper();

            var user = await _userRepository.GetUserByUserIdAsync(normalizedUserId);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User {UserId} not found.", normalizedUserId);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // Verify password using the password hasher
            if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
            {
                _logger.LogWarning("Login failed: Invalid password for user {UserId}.", normalizedUserId);
                throw new UnauthorizedAccessException("Invalid credentials.");
            }

            // If a department is not assigned, prevent login (similar to Delphi's check)
            if (string.IsNullOrWhiteSpace(user.Department))
            {
                _logger.LogWarning("Login failed: User {UserId} has no assigned department.", normalizedUserId);
                throw new UnauthorizedAccessException("User has no assigned department. Please contact administrator.");
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user.Id, user.UserId, user.Department!, user.AccessMask);

            _logger.LogInformation("User {UserId} logged in successfully. Token generated.", normalizedUserId);

            return new LoginResponse(token, user.Id.ToString(), user.Department!, user.AccessMask, user.UserId);
        }
    }
}