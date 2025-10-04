using api.Modules.Common.Data;
using api.Modules.Kappi.Repository;
using api.Modules.Storage.Services;

namespace api.Modules.Kappi.Services;

public class AnonymousGalleryCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnonymousGalleryCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public AnonymousGalleryCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<AnonymousGalleryCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Anonymous gallery cleanup service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredGalleriesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning up expired anonymous galleries");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }

    private async Task CleanupExpiredGalleriesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting expired anonymous gallery cleanup");

        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAnonymousGalleryRepository>();
        var s3Service = scope.ServiceProvider.GetRequiredService<IS3Service>();
        var db = scope.ServiceProvider.GetRequiredService<Db>();

        var expiredGalleries = await repository.GetExpiredGalleriesAsync();

        if (expiredGalleries.Count > 0)
        {
            _logger.LogInformation("Removing {Count} expired anonymous galleries", expiredGalleries.Count);

            foreach (var gallery in expiredGalleries)
            {
                try
                {
                    // Delete all media from S3
                    var s3Keys = gallery.Media.Select(m => m.S3Key).ToList();
                    if (s3Keys.Any())
                    {
                        _logger.LogInformation("Deleting {MediaCount} media files from S3 for gallery {GalleryId}", s3Keys.Count, gallery.Id);
                        await s3Service.DeleteObjectsAsync(s3Keys);
                    }

                    // Delete gallery (cascade deletes media records)
                    db.Remove(gallery);

                    _logger.LogInformation("Deleted expired anonymous gallery {GalleryId} (expired at {ExpiresAt})", gallery.Id, gallery.ExpiresAt);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting expired anonymous gallery {GalleryId}", gallery.Id);
                }
            }

            await db.SaveChangesAsync();
            _logger.LogInformation("Completed removal of expired anonymous galleries");
        }
        else
        {
            _logger.LogDebug("No expired anonymous galleries found");
        }
    }
}
