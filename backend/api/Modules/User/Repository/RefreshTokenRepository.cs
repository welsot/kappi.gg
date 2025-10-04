using api.Data;
using api.Modules.Common.Repository;
using api.Modules.User.Models;

using Microsoft.EntityFrameworkCore;

namespace api.Modules.User.Repository;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> FindByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == token);
    }

    public async Task RevokeAllUserTokensAsync(Guid userId)
    {
        await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(setter => setter
                .SetProperty(t => t.RevokedAt, DateTime.UtcNow));
    }

    public async Task<int> CountExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.RefreshTokens
            .CountAsync(t => t.ExpiresAt < now, cancellationToken);
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _context.RefreshTokens
            .Where(t => t.ExpiresAt < now)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
