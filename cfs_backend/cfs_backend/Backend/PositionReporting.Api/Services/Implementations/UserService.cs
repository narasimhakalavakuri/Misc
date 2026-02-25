using System.Security.Claims;
using PositionReporting.Api.Services.Interfaces;
using PositionReporting.Core.Utils; // For Result pattern
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;
using Kiota.ApiClient.Models; // For Kiota generated models
using System.Net; // For HttpStatusCode

namespace PositionReporting.Api.Services.Implementations;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<Infrastructure.Data.Entities.User, string>> ValidateSamlUserAsync(string externalUserId, IEnumerable<Claim> samlClaims)
    {
        _logger.LogInformation("Validating SAML user: {ExternalUserId}", externalUserId);

        var user = await _userRepository.GetByExternalIdAsync(externalUserId);

        if (user == null)
        {
            _logger.LogWarning("User '{ExternalUserId}' not found in local registry. Attempting to auto-provision.", externalUserId);
            // Auto-provisioning logic (if allowed)
            try
            {
                var newUser = new Infrastructure.Data.Entities.User
                {
                    Id = Guid.NewGuid(),
                    UserId = externalUserId, // Use external ID as primary UserID in our system, e.g., UPN
                    Email = samlClaims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
                    DepartmentId = null, // Default to null, admin to assign
                    AccessMask = "0000000", // Default to no access, admin to assign
                    IsActive = true,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                await _userRepository.AddAsync(newUser);
                _logger.LogInformation("User '{ExternalUserId}' successfully auto-provisioned.", externalUserId);
                return newUser;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-provision user '{ExternalUserId}'.", externalUserId);
                return $"Failed to provision user: {ex.Message}";
            }
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("User '{ExternalUserId}' is inactive in local registry.", externalUserId);
            return "User is inactive. Please contact your administrator.";
        }

        // Additional tenant validation can happen here if 'user' has a TenantId.
        // E.g., if (!user.TenantId.HasValue || !await _tenantService.IsTenantActive(user.TenantId.Value)) { return "Tenant inactive"; }

        _logger.LogInformation("User '{ExternalUserId}' successfully validated.", externalUserId);
        return user;
    }

    public async Task<List<Kiota.ApiClient.Models.User>> ListUsersAsync(Controllers.UsersController.UserListRequest request)
    {
        _logger.LogInformation("Listing users with filter: {Filter}", request.Filter);
        var entities = await _userRepository.GetAllAsync(request.Limit, request.Offset, request.Sort, request.Filter);
        return entities.Select(MapToKiotaUser).ToList();
    }

    public async Task<Kiota.ApiClient.Models.User?> GetUserByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving user with internal ID: {Id}", id);
        var entity = await _userRepository.GetByIdAsync(id);
        return entity == null ? null : MapToKiotaUser(entity);
    }

    public async Task<Kiota.ApiClient.Models.User> CreateUserAsync(UserCreateRequest request)
    {
        _logger.LogInformation("Creating new user: {UserId}", request.UserId);

        // Business logic: Check for uniqueness
        var existingUser = await _userRepository.GetByExternalIdAsync(request.UserId);
        if (existingUser != null)
        {
            throw new BadHttpRequestException($"User '{request.UserId}' already exists.", (int)HttpStatusCode.Conflict);
        }

        var newEntity = new Infrastructure.Data.Entities.User
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DepartmentId = null, // Default to null
            AccessMask = "0000000", // Default to no permissions
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await _userRepository.AddAsync(newEntity);
        return MapToKiotaUser(newEntity);
    }

    public async Task<Kiota.ApiClient.Models.User?> UpdateUserAsync(string userId, UserUpdateRequest request)
    {
        _logger.LogInformation("Updating user: {UserId}", userId);
        var entity = await _userRepository.GetByExternalIdAsync(userId);
        if (entity == null) return null;

        // Business logic: Validate access mask combinations
        if (request.AccessMask != null)
        {
            ValidateAccessMask(request.AccessMask);
            entity.AccessMask = ConvertAccessMaskToString(request.AccessMask);
        }

        entity.DepartmentId = request.DepartmentId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _userRepository.UpdateAsync(entity);
        return MapToKiotaUser(entity);
    }

    public async Task DeleteUserAsync(string userId)
    {
        _logger.LogInformation("Deleting user: {UserId}", userId);
        var entity = await _userRepository.GetByExternalIdAsync(userId);
        if (entity == null)
        {
            throw new BadHttpRequestException($"User '{userId}' not found.", (int)HttpStatusCode.NotFound);
        }

        await _userRepository.DeleteAsync(entity.Id);
    }

    // --- Helper Methods ---
    private void ValidateAccessMask(AccessMask accessMask)
    {
        // "Administrative access flags (Admin, User Admin, System Control) are mutually exclusive with other fine-grained permissions."
        bool isAdminOrUserAdminOrSysControl = (accessMask.IsAdmin ?? false) || (accessMask.IsUserAdmin ?? false) || (accessMask.CanSysControl ?? false);
        bool hasFineGrainedPermissions = (accessMask.CanQuery ?? false) || (accessMask.CanInput ?? false) || (accessMask.CanCheck ?? false) || (accessMask.CanReport ?? false);

        if (isAdminOrUserAdminOrSysControl && hasFineGrainedPermissions)
        {
            throw new BadHttpRequestException("Invalid access mask combination. Administrative access flags are mutually exclusive with other fine-grained permissions.", (int)HttpStatusCode.UnprocessableEntity);
        }
    }

    private string ConvertAccessMaskToString(AccessMask accessMask)
    {
        // Delphi legacy system uses a string of '0's and '1's.
        // Assuming a fixed order: Q,I,C,A,U,R,S
        var sb = new StringBuilder();
        sb.Append((accessMask.CanQuery ?? false) ? '1' : '0');
        sb.Append((accessMask.CanInput ?? false) ? '1' : '0');
        sb.Append((accessMask.CanCheck ?? false) ? '1' : '0');
        sb.Append((accessMask.IsAdmin ?? false) ? '1' : '0');
        sb.Append((accessMask.IsUserAdmin ?? false) ? '1' : '0');
        sb.Append((accessMask.CanReport ?? false) ? '1' : '0');
        sb.Append((accessMask.CanSysControl ?? false) ? '1' : '0');
        return sb.ToString();
    }

    private AccessMask ConvertStringToAccessMask(string? accessMaskString)
    {
        if (string.IsNullOrEmpty(accessMaskString))
        {
            return new AccessMask(); // Default to all false
        }

        var accessMask = new AccessMask();
        if (accessMaskString.Length > 0) accessMask.CanQuery = accessMaskString[0] == '1';
        if (accessMaskString.Length > 1) accessMask.CanInput = accessMaskString[1] == '1';
        if (accessMaskString.Length > 2) accessMask.CanCheck = accessMaskString[2] == '1';
        if (accessMaskString.Length > 3) accessMask.IsAdmin = accessMaskString[3] == '1';
        if (accessMaskString.Length > 4) accessMask.IsUserAdmin = accessMaskString[4] == '1';
        if (accessMaskString.Length > 5) accessMask.CanReport = accessMaskString[5] == '1';
        if (accessMaskString.Length > 6) accessMask.CanSysControl = accessMaskString[6] == '1';
        return accessMask;
    }


    // --- Mapping Functions (Entity to Kiota DTOs) ---
    private Kiota.ApiClient.Models.User MapToKiotaUser(Infrastructure.Data.Entities.User entity)
    {
        return new Kiota.ApiClient.Models.User
        {
            Id = entity.Id.ToString(),
            UserId = entity.UserId,
            DepartmentId = entity.DepartmentId,
            AccessMask = ConvertStringToAccessMask(entity.AccessMask)
        };
    }
}