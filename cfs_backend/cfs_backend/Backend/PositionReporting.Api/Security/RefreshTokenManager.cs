using System.Security.Cryptography;
using Kiota.ApiClient.Models;
using Microsoft.Extensions.Configuration;
using PositionReporting.Api.Models.Auth;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;
using PositionReporting.Core.Utils; // Assume this utility class for Result pattern

namespace PositionReporting.Api.Security;

public class RefreshTokenManager
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserRepository _userRepository; // For user validation
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly int _refreshTokenExpirationDays;
    private readonly int _refreshTokenLength;
    private readonly ILogger<RefreshTokenManager> _logger;

    public RefreshTokenManager(
        IRefreshTokenRepository refreshTokenRepository,
        IUserRepository userRepository,
        JwtTokenGenerator jwtTokenGenerator,
        IConfiguration configuration,
        ILogger<RefreshTokenManager> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenExpirationDays = configuration.GetValue<int>("TokenManagement:RefreshTokenExpirationDays", 7);
        _refreshTokenLength = configuration.GetValue<int>("TokenManagement:RefreshTokenLength", 64);
        _logger = logger;
    }

    /// <summary>
    /// Generates a new high-entropy refresh token string.
    /// </summary>
    /// <returns>The generated refresh token string.</returns>
    public string GenerateRefreshTokenString()
    {
        var randomNumber = new byte[_refreshTokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    /// <summary>
    /// Hashes a refresh token string for secure storage.
    /// </summary>
    /// <param name="token">The refresh token string.</param>
    /// <returns>The hashed refresh token.</returns>
    public string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Creates and stores a new refresh token for a user, revoking any old ones if required.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <param name="currentRefreshTokenHash">The hash of the refresh token being replaced (for rotation).</param>
    /// <returns>The new refresh token string and its expiration.</returns>
    public async Task<(string newToken, DateTimeOffset expiresAt)> CreateAndStoreRefreshTokenAsync(Guid userId, string? currentRefreshTokenHash = null)
    {
        var refreshTokenString = GenerateRefreshTokenString();
        var refreshTokenHash = HashRefreshToken(refreshTokenString);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_refreshTokenExpirationDays);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = refreshTokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow,
            IsRevoked = false,
            ReplacedByTokenHash = null // No replacement initially
        };

        if (!string.IsNullOrEmpty(currentRefreshTokenHash))
        {
            // If rotating, mark the old token as replaced
            var oldToken = await _refreshTokenRepository.GetByTokenHashAsync(currentRefreshTokenHash);
            if (oldToken != null)
            {
                oldToken.IsRevoked = true;
                oldToken.ReplacedByTokenHash = refreshTokenHash;
                await _refreshTokenRepository.UpdateAsync(oldToken);
            }
        }

        await _refreshTokenRepository.AddAsync(refreshToken);
        return (refreshTokenString, expiresAt);
    }

    /// <summary>
    /// Validates a refresh token, rotates it, and generates a new access token and refresh token.
    /// </summary>
    /// <param name="refreshTokenString">The refresh token string provided by the client.</param>
    /// <returns>A Result containing the new TokenResponse or an Error.</returns>
    public async Task<Result<TokenResponse, TokenError>> ValidateAndRotateRefreshTokenAsync(string refreshTokenString)
    {
        var incomingTokenHash = HashRefreshToken(refreshTokenString);
        var existingRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(incomingTokenHash);

        if (existingRefreshToken == null)
        {
            _logger.LogWarning("Attempted refresh with non-existent token hash.");
            return new TokenError("Refresh token does not exist.", TokenErrorCodes.InvalidToken);
        }

        if (existingRefreshToken.IsRevoked)
        {
            // Replay attack detected. Revoke entire token family for this user.
            _logger.LogCritical("Refresh token replay attack detected for user {UserId}. Revoking entire token family.", existingRefreshToken.UserId);
            await RevokeAllRefreshTokensForUserAsync(existingRefreshToken.UserId);
            return new TokenError("Refresh token has been revoked or replayed.", TokenErrorCodes.TokenReplayed);
        }

        if (existingRefreshToken.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Expired refresh token used for user {UserId}.", existingRefreshToken.UserId);
            // Mark the expired token as revoked to prevent further use
            existingRefreshToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(existingRefreshToken);
            return new TokenError("Refresh token has expired.", TokenErrorCodes.ExpiredToken);
        }

        var user = await _userRepository.GetByIdAsync(existingRefreshToken.UserId);
        if (user == null || !user.IsActive) // Assume IsActive property on User entity
        {
            _logger.LogWarning("Refresh token for non-existent or inactive user {UserId}.", existingRefreshToken.UserId);
            existingRefreshToken.IsRevoked = true;
            await _refreshTokenRepository.UpdateAsync(existingRefreshToken);
            return new TokenError("Associated user not found or is inactive.", TokenErrorCodes.UserNotFound);
        }

        // Generate new refresh token and revoke the old one
        var (newRefreshTokenString, newRefreshTokenExpiresAt) = await CreateAndStoreRefreshTokenAsync(existingRefreshToken.UserId, incomingTokenHash);

        // Generate new access token
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserId), // Assuming UserId is like DOMAIN\USERNAME
            new Claim(ClaimTypes.Email, user.Email ?? ""), // Assuming Email property
            // Add other relevant user claims from the user entity, e.g., roles, departmentId
            new Claim("departmentId", user.DepartmentId ?? ""),
            new Claim("accessmask", user.AccessMask ?? "") // Assuming a combined access mask string
        };

        var accessTokenResponse = _jwtTokenGenerator.GenerateAccessToken(claims, user.Id);

        var tokenResponse = new TokenResponse(
            AccessToken: accessTokenResponse.AccessToken,
            AccessTokenExpiresAt: accessTokenResponse.AccessTokenExpiresAt,
            RefreshToken: newRefreshTokenString,
            RefreshTokenExpiresAt: newRefreshTokenExpiresAt
        );

        _logger.LogInformation("Refresh token successfully rotated for user {UserId}.", existingRefreshToken.UserId);
        return tokenResponse;
    }

    /// <summary>
    /// Revokes all refresh tokens for a given user.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    public async Task RevokeAllRefreshTokensForUserAsync(Guid userId)
    {
        await _refreshTokenRepository.RevokeAllTokensForUserAsync(userId);
        _logger.LogInformation("All refresh tokens revoked for user {UserId}.", userId);
    }
}

// Custom error structure for token operations
public record TokenError(string Message, string ErrorCode);

public static class TokenErrorCodes
{
    public const string InvalidToken = "InvalidToken";
    public const string ExpiredToken = "ExpiredToken";
    public const string TokenReplayed = "TokenReplayed";
    public const string UserNotFound = "UserNotFound";
    public const string InternalError = "InternalError";
}

// Simple Result pattern utility (assumed to exist in Core.Utils)
namespace PositionReporting.Core.Utils
{
    public abstract class Result<TValue, TError>
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public TValue Value { get; }
        public TError Error { get; }

        protected Result(TValue value, TError error, bool isSuccess)
        {
            Value = value;
            Error = error;
            IsSuccess = isSuccess;
        }

        public static implicit operator Result<TValue, TError>(TValue value) => Success(value);
        public static implicit operator Result<TValue, TError>(TError error) => Failure(error);

        public static Result<TValue, TError> Success(TValue value) => new SuccessResult(value);
        public static Result<TValue, TError> Failure(TError error) => new FailureResult(error);

        private sealed class SuccessResult : Result<TValue, TError>
        {
            public SuccessResult(TValue value) : base(value, default!, true) { }
        }

        private sealed class FailureResult : Result<TValue, TError>
        {
            public FailureResult(TError error) : base(default!, error, false) { }
        }
    }
}