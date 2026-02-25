using System.Security.Claims;
using PositionReporting.Api.Models.Auth;
using PositionReporting.Api.Security; // For TokenError
using PositionReporting.Core.Utils; // For Result pattern

namespace PositionReporting.Api.Services.Interfaces;

public interface ITokenService
{
    Task<Result<TokenResponse, string>> GenerateTokensAsync(Guid userId, IEnumerable<Claim> claims);
    Task<Result<TokenResponse, TokenError>> RefreshAccessTokenAsync(string refreshToken);
    Task RevokeAllRefreshTokensForUserAsync(Guid userId);
}