using api.Modules.Common.Repository;
using api.Modules.User.Models;

namespace api.Modules.User.Repository;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> FindByTokenAsync(string token);
    Task RevokeAllUserTokensAsync(Guid userId);
    Task<int> CountExpiredAsync(CancellationToken cancellationToken = default);
    Task DeleteExpiredAsync(CancellationToken cancellationToken = default);
}
