using Microsoft.EntityFrameworkCore;
using PositionReporting.Infrastructure.Data;
using PositionReporting.Infrastructure.Data.Entities;
using PositionReporting.Infrastructure.Repositories.Interfaces;

namespace PositionReporting.Infrastructure.Repositories.Implementations;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RefreshTokenRepository> _logger;

    public RefreshTokenRepository(ApplicationDbContext context, ILogger<RefreshTokenRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
    {
        _logger.LogDebug("Retrieving refresh token by hash.");
        return await _context.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
    }

    public async Task AddAsync(RefreshToken refreshToken)
    {
        _logger.LogDebug("Adding new refresh token for user {UserId}.", refreshToken.UserId);
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RefreshToken refreshToken)
    {
        _logger.LogDebug("Updating refresh token {Id} for user {UserId}.", refreshToken.Id, refreshToken.UserId);
        _context.RefreshTokens.Update(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeAllTokensForUserAsync(Guid userId)
    {
        _logger.LogInformation("Revoking all refresh tokens for user {UserId}.", userId);
        var tokens = await _context.RefreshTokens.Where(rt => rt.UserId == userId && !rt.IsRevoked).ToListAsync();
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
            token.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await _context.SaveChangesAsync();
    }
}