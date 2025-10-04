using api.Data;
using api.Modules.Common.Repository;
using api.Modules.Kappi.Models;

using Microsoft.EntityFrameworkCore;

namespace api.Modules.Kappi.Repository;

public class AnonymousGalleryRepository(ApplicationDbContext context)
    : BaseRepository<AnonymousGallery>(context), IAnonymousGalleryRepository
{
    public async Task<AnonymousGallery?> FindByIdAsync(Guid id)
    {
        return await _context.AnonymousGalleries
            .Include(g => g.Media)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<AnonymousGallery?> FindByShortCodeAsync(string shortCode)
    {
        return await _context.AnonymousGalleries
            .Include(g => g.Media)
            .FirstOrDefaultAsync(g => g.ShortCode == shortCode);
    }

    public async Task<AnonymousGallery?> FindByAccessKeyAsync(string accessKey)
    {
        return await _context.AnonymousGalleries
            .Include(g => g.Media)
            .FirstOrDefaultAsync(g => g.AccessKey == accessKey);
    }

    public async Task<bool> ExistsByShortCodeAsync(string shortCode)
    {
        return await _context.AnonymousGalleries
            .AnyAsync(g => g.ShortCode == shortCode);
    }

    public async Task<List<AnonymousGallery>> GetExpiredGalleriesAsync()
    {
        return await _context.AnonymousGalleries
            .Include(g => g.Media)
            .Where(g => g.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync();
    }
}
