using System.Security.Claims;
using PositionReporting.Api.Models.Auth;
using PositionReporting.Api.Security;
using PositionReporting.Api.Services.Interfaces;
using PositionReporting.Core.Utils; // For Result pattern

namespace PositionReporting.Api.Services.Implementations;

public class TokenService : ITokenService
{
    private readonly JwtTokenGenerator _jwtTokenGenerator;
    private readonly RefreshTokenManager _refreshTokenManager;
    private readonly ILogger<TokenService> _logger;

    public TokenService(JwtTokenGenerator jwtTokenGenerator, RefreshTokenManager refreshTokenManager, ILogger<TokenService> logger)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _refreshTokenManager = refreshTokenManager;
        _logger = logger;
    }

    public async Task<Result<TokenResponse, string>> GenerateTokensAsync(Guid userId, IEnumerable<Claim> claims)
    {
        try
        {
            var accessTokenResponse = _jwtTokenGenerator.GenerateAccessToken(claims, userId);
            var (refreshTokenString, refreshTokenExpiresAt) = await _refreshTokenManager.CreateAndStoreRefreshTokenAsync(userId);

            var tokenResponse = accessTokenResponse with
            {
                RefreshToken = refreshTokenString,
                RefreshTokenExpiresAt = refreshTokenExpiresAt
            };

            _logger.LogInformation("Tokens generated successfully for user {UserId}.", userId);
            return tokenResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tokens for user {UserId}.", userId);
            return $"Failed to generate tokens: {ex.Message}";
        }
    }

    public async Task<Result<TokenResponse, TokenError>> RefreshAccessTokenAsync(string refreshToken)
    {
        _logger.LogInformation("Attempting to refresh access token.");
        return await _refreshTokenManager.ValidateAndRotateRefreshTokenAsync(refreshToken);
    }

    public async Task RevokeAllRefreshTokensForUserAsync(Guid userId)
    {
        await _refreshTokenManager.RevokeAllRefreshTokensForUserAsync(userId);
    }
}