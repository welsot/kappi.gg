using api.Modules.Common.Repository;
using api.Modules.Kappi.Models;

namespace api.Modules.Kappi.Repository;

public interface IMediaRepository : IRepository<Media>
{
    Task<Media?> FindByIdAsync(Guid id);
    Task<Media?> FindByS3KeyAsync(string s3Key);
    Task<List<Media>> GetByAnonymousGalleryIdAsync(Guid galleryId);
    Task<List<Media>> GetByGalleryIdAsync(Guid galleryId);
}
