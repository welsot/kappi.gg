using api.Modules.Kappi.Models;
using api.Modules.Kappi.Repository;
using api.Modules.Storage.Services;

namespace api.Modules.Kappi.Services;

public class MediaMetadataService(
    IS3Service s3Service,
    IMediaRepository mediaRepository,
    ILogger<MediaMetadataService> logger
)
{
    public async Task<bool> FetchAndStoreMetadataAsync(Media media)
    {
        try
        {
            // Check if metadata already exists
            if (media.MediaType != null && media.Width != null && media.Height != null)
            {
                logger.LogDebug("Metadata already exists for media {MediaId}", media.Id);
                return true;
            }

            logger.LogInformation("Fetching metadata from S3 for media {MediaId} with key {S3Key}", media.Id, media.S3Key);

            // Fetch tags from S3
            var tags = await s3Service.GetObjectTagsAsync(media.S3Key);

            if (tags == null)
            {
                logger.LogWarning("Could not fetch tags for media {MediaId} - S3 object not found", media.Id);
                return false;
            }

            // Extract metadata from tags
            string? mediaType = tags.GetValueOrDefault("media_type") ?? tags.GetValueOrDefault("MediaType");
            int? width = ParseIntTag(tags, "width") ?? ParseIntTag(tags, "Width");
            int? height = ParseIntTag(tags, "height") ?? ParseIntTag(tags, "Height");

            // Get file size from S3
            long? fileSize = await s3Service.GetFileSizeAsync(media.S3Key);

            if (mediaType == null && width == null && height == null && fileSize == null)
            {
                logger.LogWarning("No metadata tags found for media {MediaId}", media.Id);
                return false;
            }

            // Update media metadata
            media.SetMetadata(mediaType, width, height, fileSize);

            logger.LogInformation(
                "Successfully fetched metadata for media {MediaId}: type={MediaType}, dimensions={Width}x{Height}, size={FileSize}",
                media.Id, mediaType, width, height, fileSize
            );

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching metadata for media {MediaId}", media.Id);
            return false;
        }
    }

    private static int? ParseIntTag(Dictionary<string, string> tags, string key)
    {
        if (tags.TryGetValue(key, out var value) && int.TryParse(value, out var result))
        {
            return result;
        }
        return null;
    }
}
