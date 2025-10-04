using api.Data;
using api.Modules.Common.Repository;
using api.Modules.Kappi.Models;

using Microsoft.EntityFrameworkCore;

namespace api.Modules.Kappi.Repository;

public class MediaRepository(ApplicationDbContext context)
    : BaseRepository<Media>(context), IMediaRepository
{
    public async Task<Media?> FindByIdAsync(Guid id)
    {
        return await _context.Media
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<Media?> FindByS3KeyAsync(string s3Key)
    {
        return await _context.Media
            .FirstOrDefaultAsync(m => m.S3Key == s3Key);
    }

    public async Task<List<Media>> GetByAnonymousGalleryIdAsync(Guid galleryId)
    {
        return await _context.Media
            .Where(m => m.AnonymousGalleryId == galleryId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Media>> GetByGalleryIdAsync(Guid galleryId)
    {
        return await _context.Media
            .Where(m => m.GalleryId == galleryId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }
}
