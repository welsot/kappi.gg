using api.Modules.Common.Repository;
using api.Modules.Kappi.Models;

namespace api.Modules.Kappi.Repository;

public interface IGalleryRepository : IRepository<Gallery>
{
    Task<Gallery?> FindByIdAsync(Guid id);
    Task<Gallery?> FindByShortCodeAsync(string shortCode);
    Task<bool> ExistsByShortCodeAsync(string shortCode);
    Task<List<Gallery>> GetByUserIdAsync(Guid userId);
    Task<Gallery?> FindByIdAndUserIdAsync(Guid id, Guid userId);
}
