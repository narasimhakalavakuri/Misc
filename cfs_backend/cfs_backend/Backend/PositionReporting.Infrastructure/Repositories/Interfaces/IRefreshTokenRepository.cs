using PositionReporting.Infrastructure.Data.Entities;

namespace PositionReporting.Infrastructure.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);
    Task AddAsync(RefreshToken refreshToken);
    Task UpdateAsync(RefreshToken refreshToken);
    Task RevokeAllTokensForUserAsync(Guid userId);
}