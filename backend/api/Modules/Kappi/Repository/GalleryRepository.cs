using api.Data;
using api.Modules.Common.Repository;
using api.Modules.Kappi.Models;

using Microsoft.EntityFrameworkCore;

namespace api.Modules.Kappi.Repository;

public class GalleryRepository(ApplicationDbContext context)
    : BaseRepository<Gallery>(context), IGalleryRepository
{
    public async Task<Gallery?> FindByIdAsync(Guid id)
    {
        return await _context.Galleries
            .Include(g => g.Media)
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<Gallery?> FindByShortCodeAsync(string shortCode)
    {
        return await _context.Galleries
            .Include(g => g.Media)
            .Include(g => g.User)
            .FirstOrDefaultAsync(g => g.ShortCode == shortCode);
    }

    public async Task<bool> ExistsByShortCodeAsync(string shortCode)
    {
        return await _context.Galleries
            .AnyAsync(g => g.ShortCode == shortCode);
    }

    public async Task<List<Gallery>> GetByUserIdAsync(Guid userId)
    {
        return await _context.Galleries
            .Include(g => g.Media)
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<Gallery?> FindByIdAndUserIdAsync(Guid id, Guid userId)
    {
        return await _context.Galleries
            .Include(g => g.Media)
            .FirstOrDefaultAsync(g => g.Id == id && g.UserId == userId);
    }
}
