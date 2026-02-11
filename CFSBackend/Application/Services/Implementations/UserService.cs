using AutoMapper;
using ProjectName.Application.Models.Users;
using ProjectName.Application.Services.Interfaces;
using ProjectName.Domain.Constants;
using ProjectName.Infrastructure.Auth;
using ProjectName.Infrastructure.Data.Entities;
using ProjectName.Infrastructure.Data.Repositories.Interfaces;
using ProjectName.Infrastructure.Exceptions;

namespace ProjectName.Application.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IDepartmentRepository _departmentRepository;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, IAuditLogRepository auditLogRepository, IDepartmentRepository departmentRepository, ICurrencyRepository currencyRepository, IPasswordHasher passwordHasher, IMapper mapper, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _auditLogRepository = auditLogRepository;
            _departmentRepository = departmentRepository;
            _currencyRepository = currencyRepository;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            _logger.LogInformation("Fetching all users.");
            var users = await _userRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<UserDto>>(users);
        }

        public async Task<UserDto?> GetUserByIdAsync(Guid id)
        {
            _logger.LogInformation("Fetching user by ID: {Id}", id);
            var user = await _userRepository.GetByIdAsync(id);
            return _mapper.Map<UserDto?>(user);
        }

        public async Task<UserDto> CreateInitialAdminUserAsync(string userId, string password, string accessMask)
        {
            // This is a special method for initial setup, bypasses some checks.
            _logger.LogInformation("Creating initial admin user: {UserId}", userId);

            // Normalize UserId to uppercase as per Delphi app
            var normalizedUserId = userId.ToUpper();

            if (await _userRepository.GetUserByUserIdAsync(normalizedUserId) != null)
            {
                throw new ValidationException($"User {normalizedUserId} already exists.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                UserId = normalizedUserId,
                PasswordHash = _passwordHasher.HashPassword(password),
                Department = "ADMIN", // Default admin dept for initial setup
                AccessMask = accessMask,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionCreateUser, $"Initial admin user {normalizedUserId} created.", "SYSTEM");

            _logger.LogInformation("Initial admin user {UserId} created successfully.", normalizedUserId);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string creatorUserId)
        {
            _logger.LogInformation("Creating new user: {UserId} by {CreatorUserId}", request.UserId, creatorUserId);

            // Normalize UserId to uppercase
            var normalizedUserId = request.UserId.ToUpper();

            if (await _userRepository.GetUserByUserIdAsync(normalizedUserId) != null)
            {
                throw new ValidationException($"User {normalizedUserId} already exists.");
            }

            if (request.Department != null)
            {
                var department = await _departmentRepository.GetByDeptCodeAsync(request.Department);
                if (department == null)
                {
                    throw new ValidationException($"Department '{request.Department}' does not exist.");
                }
            }

            var user = _mapper.Map<User>(request);
            user.UserId = normalizedUserId;
            user.PasswordHash = _passwordHasher.HashPassword(request.Password);
            user.CreatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(user);
            await _userRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionCreateUser, $"User {normalizedUserId} created by {creatorUserId}.", creatorUserId);

            _logger.LogInformation("User {UserId} created successfully by {CreatorUserId}.", normalizedUserId, creatorUserId);
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto?> UpdateUserAsync(Guid id, UpdateUserRequest request, string updaterUserId)
        {
            _logger.LogInformation("Updating user {Id} by {UpdaterUserId}", id, updaterUserId);

            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User {Id} not found for update.", id);
                throw new NotFoundException($"User with ID {id} not found.");
            }

            if (request.Department != null)
            {
                var department = await _departmentRepository.GetByDeptCodeAsync(request.Department);
                if (department == null)
                {
                    throw new ValidationException($"Department '{request.Department}' does not exist.");
                }
            }

            _mapper.Map(request, existingUser);
            existingUser.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(existingUser);
            await _userRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionUpdateUser, $"User {existingUser.UserId} (ID: {id}) updated by {updaterUserId}.", updaterUserId);

            _logger.LogInformation("User {Id} updated successfully by {UpdaterUserId}.", id, updaterUserId);
            return _mapper.Map<UserDto>(existingUser);
        }

        public async Task<bool> DeleteUserAsync(Guid id, string deleterUserId)
        {
            _logger.LogInformation("Deleting user {Id} by {DeleterUserId}", id, deleterUserId);

            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                _logger.LogWarning("User {Id} not found for deletion.", id);
                return false;
            }

            await _userRepository.DeleteAsync(existingUser);
            await _userRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionDeleteUser, $"User {existingUser.UserId} (ID: {id}) deleted by {deleterUserId}.", deleterUserId);

            _logger.LogInformation("User {Id} deleted successfully by {DeleterUserId}.", id, deleterUserId);
            return true;
        }

        public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword, string changerUserId)
        {
            _logger.LogInformation("Attempting to change password for user {UserId} by {ChangerUserId}.", userId, changerUserId);

            if (newPassword != newPassword) // Assuming newPassword == ConfirmPassword check in controller/request validation
            {
                throw new ValidationException("New password and confirm password do not match.");
            }

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ValidationException("Old and new passwords cannot be empty.");
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for password change.", userId);
                throw new NotFoundException($"User with ID {userId} not found.");
            }

            // A user can only change their own password
            var changerUser = await _userRepository.GetByIdAsync(Guid.Parse(changerUserId));
            if (changerUser == null)
            {
                throw new UnauthorizedAccessException("Changer user not found.");
            }

            // if user.Id != Guid.Parse(changerUserId) // Uncomment if only self-password change is allowed
            // {
            //     throw new UnauthorizedAccessException("You can only change your own password.");
            // }

            if (!_passwordHasher.VerifyPassword(user.PasswordHash, oldPassword))
            {
                _logger.LogWarning("Old password mismatch for user {UserId}.", userId);
                throw new UnauthorizedAccessException("Old password is wrong!");
            }

            user.PasswordHash = _passwordHasher.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            await _userRepository.SaveAsync();

            await _auditLogRepository.LogEntryAsync(AuditConstants.ActionChangePassword, $"Password changed for user {user.UserId} (ID: {userId}) by {changerUserId}.", changerUserId);

            _logger.LogInformation("Password changed successfully for user {UserId}.", userId);
        }

        public async Task SeedDefaultDepartments()
        {
            if (!(await _departmentRepository.GetAllAsync()).Any())
            {
                var defaultDept = new Department {
                    DeptCode = "DEFAULT",
                    DeptDesc = "Default Department",
                    ApprType = "N",
                    RefLock = ".",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _departmentRepository.AddAsync(defaultDept);
                await _departmentRepository.SaveAsync();
                _logger.LogInformation("Seeded default department 'DEFAULT'.");
            }
        }

        public async Task SeedDefaultCurrencies()
        {
            if (!(await _currencyRepository.GetAllAsync()).Any())
            {
                var defaultCurrencies = new List<Currency>
                {
                    new Currency { CurrCode = "SGD", CurrDesc = "Singapore Dollar", Deciml = 2, Tts = 1.00m, Sts = 1.00m, Bts = 1.00m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Currency { CurrCode = "USD", CurrDesc = "US Dollar", Deciml = 2, Tts = 1.35m, Sts = 1.36m, Bts = 1.34m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Currency { CurrCode = "EUR", CurrDesc = "Euro", Deciml = 2, Tts = 1.48m, Sts = 1.49m, Bts = 1.47m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new Currency { CurrCode = "JPY", CurrDesc = "Japanese Yen", Deciml = 0, Tts = 0.009m, Sts = 0.0091m, Bts = 0.0089m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                };
                await _currencyRepository.AddRangeAsync(defaultCurrencies);
                await _currencyRepository.SaveAsync();
                _logger.LogInformation("Seeded default currencies.");
            }
        }
    }
}