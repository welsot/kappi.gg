using api.Modules.Common.Repository;
using api.Modules.Kappi.Models;

namespace api.Modules.Kappi.Repository;

public interface IAnonymousGalleryRepository : IRepository<AnonymousGallery>
{
    Task<AnonymousGallery?> FindByIdAsync(Guid id);
    Task<AnonymousGallery?> FindByShortCodeAsync(string shortCode);
    Task<AnonymousGallery?> FindByAccessKeyAsync(string accessKey);
    Task<bool> ExistsByShortCodeAsync(string shortCode);
    Task<List<AnonymousGallery>> GetExpiredGalleriesAsync();
}
