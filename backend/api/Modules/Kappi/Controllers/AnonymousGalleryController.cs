using api.Modules.Common.Controllers;
using api.Modules.Common.Data;
using api.Modules.Common.DTO;
using api.Modules.Common.Services;
using api.Modules.Kappi.DTOs;
using api.Modules.Kappi.Models;
using api.Modules.Kappi.Repository;
using api.Modules.Kappi.Services;
using api.Modules.Storage.Services;

using Microsoft.AspNetCore.Mvc;

namespace api.Modules.Kappi.Controllers;

[ApiController]
public class AnonymousGalleryController(
    Db db,
    ILogger<AnonymousGalleryController> logger,
    IAnonymousGalleryRepository anonymousGalleries,
    IMediaRepository mediaRepository,
    ShortCodeGenerator shortCodeGenerator,
    MediaMetadataService metadataService,
    IS3Service s3Service
) : ApiController
{
    [HttpPost("api/galleries/anonymous")]
    [EndpointName("createAnonymousGallery")]
    [ProducesResponseType(typeof(CreateAnonymousGalleryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAnonymousGallery()
    {
        try
        {
            var shortCode = await shortCodeGenerator.GenerateUniqueShortCodeAsync();
            var accessKey = RandomTokenGenerator.GenerateRandomToken();

            var gallery = new AnonymousGallery(Guid.NewGuid(), shortCode, accessKey);
            anonymousGalleries.Add(gallery);
            await db.SaveChangesAsync();

            logger.LogInformation("Created anonymous gallery {GalleryId} with short code {ShortCode}", gallery.Id, shortCode);

            return Created(new CreateAnonymousGalleryResponse(
                gallery.Id,
                gallery.ShortCode,
                gallery.AccessKey,
                gallery.ExpiresAt
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating anonymous gallery");
            return Error(500, "failed_to_create_gallery");
        }
    }

    [HttpPost("api/galleries/anonymous/{accessKey}/media/request-upload")]
    [EndpointName("anonymousGalleryRequestUploadUrl")]
    [ProducesResponseType(typeof(UploadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestUploadUrl(
        string accessKey,
        [FromBody] RequestUploadUrlDto dto
    )
    {
        try
        {
            var gallery = await anonymousGalleries.FindByAccessKeyAsync(accessKey);
            if (gallery == null)
            {
                logger.LogWarning("Anonymous gallery not found with access key");
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            // Check if gallery is expired
            if (gallery.ExpiresAt <= DateTime.UtcNow)
            {
                logger.LogWarning("Anonymous gallery {GalleryId} is expired", gallery.Id);
                return Error(410, "gallery_expired");
            }

            // Generate S3 key
            var s3Key = $"galleries/anonymous/{gallery.Id}/{Guid.NewGuid()}-{dto.FileName}";

            // Create media record
            var media = new Media(Guid.NewGuid(), gallery.Id, s3Key);
            mediaRepository.Add(media);
            await db.SaveChangesAsync();

            // Generate pre-signed upload URL
            var uploadUrl = await s3Service.GeneratePresignedUploadUrlAsync(s3Key, dto.ContentType);

            logger.LogInformation("Generated upload URL for media {MediaId} in anonymous gallery {GalleryId}", media.Id, gallery.Id);

            return Ok(new UploadUrlResponse(media.Id, uploadUrl, s3Key));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error requesting upload URL for anonymous gallery");
            return Error(500, "failed_to_generate_upload_url");
        }
    }

    [HttpPost("api/galleries/anonymous/{accessKey}/media/confirm-upload")]
    [EndpointName("anonymousGalleryConfirmUpload")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmUpload(
        string accessKey,
        [FromBody] ConfirmUploadDto dto
    )
    {
        try
        {
            var gallery = await anonymousGalleries.FindByAccessKeyAsync(accessKey);
            if (gallery == null)
            {
                logger.LogWarning("Anonymous gallery not found with access key");
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            var media = await mediaRepository.FindByIdAsync(dto.MediaId);
            if (media == null || media.AnonymousGalleryId != gallery.Id)
            {
                logger.LogWarning("Media {MediaId} not found in anonymous gallery {GalleryId}", dto.MediaId, gallery.Id);
                return NotFound(new ErrorResponse("media_not_found"));
            }

            // Check if file exists in S3
            var exists = await s3Service.KeyExistsAsync(media.S3Key);
            if (!exists)
            {
                logger.LogWarning("Media {MediaId} not found in S3", media.Id);
                return NotFound(new ErrorResponse("media_not_uploaded"));
            }

            // Fetch and store metadata
            await metadataService.FetchAndStoreMetadataAsync(media);
            await db.SaveChangesAsync();

            logger.LogInformation("Confirmed upload for media {MediaId} in anonymous gallery {GalleryId}", media.Id, gallery.Id);

            return Ok(new SuccessResponse("success"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming upload for anonymous gallery");
            return Error(500, "failed_to_confirm_upload");
        }
    }

    [HttpGet("api/galleries/anonymous/by-short-code/{shortCode}")]
    [EndpointName("getAnonymousGalleryByShortCode")]
    [ProducesResponseType(typeof(AnonymousGalleryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByShortCode(string shortCode)
    {
        try
        {
            var gallery = await anonymousGalleries.FindByShortCodeAsync(shortCode);
            if (gallery == null)
            {
                logger.LogWarning("Anonymous gallery not found with short code {ShortCode}", shortCode);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            // Check if gallery is expired
            if (gallery.ExpiresAt <= DateTime.UtcNow)
            {
                logger.LogWarning("Anonymous gallery {GalleryId} is expired", gallery.Id);
                return Error(410, "gallery_expired");
            }

            // Generate download URLs for all media
            var mediaDtos = new List<MediaDto>();
            foreach (var media in gallery.Media)
            {
                // Fetch metadata if not present
                if (media.MediaType == null)
                {
                    await metadataService.FetchAndStoreMetadataAsync(media);
                    await db.SaveChangesAsync();
                }

                var downloadUrl = await s3Service.GeneratePresignedDownloadUrlAsync(media.S3Key);
                mediaDtos.Add(new MediaDto(
                    media.Id,
                    media.MediaType,
                    media.Width,
                    media.Height,
                    media.FileSize,
                    downloadUrl,
                    media.CreatedAt
                ));
            }

            logger.LogInformation("Retrieved anonymous gallery {GalleryId} by short code {ShortCode}", gallery.Id, shortCode);

            return Ok(new AnonymousGalleryDto(
                gallery.Id,
                gallery.ShortCode,
                gallery.ExpiresAt,
                gallery.CreatedAt,
                new MediaListResponse(mediaDtos, mediaDtos.Count)
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting anonymous gallery by short code {ShortCode}", shortCode);
            return Error(500, "failed_to_get_gallery");
        }
    }

    [HttpDelete("api/galleries/anonymous/{accessKey}/media/{mediaId}")]
    [EndpointName("deleteAnonymousGalleryMedia")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMedia(string accessKey, Guid mediaId)
    {
        try
        {
            var gallery = await anonymousGalleries.FindByAccessKeyAsync(accessKey);
            if (gallery == null)
            {
                logger.LogWarning("Anonymous gallery not found with access key");
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            var media = await mediaRepository.FindByIdAsync(mediaId);
            if (media == null || media.AnonymousGalleryId != gallery.Id)
            {
                logger.LogWarning("Media {MediaId} not found in anonymous gallery {GalleryId}", mediaId, gallery.Id);
                return NotFound(new ErrorResponse("media_not_found"));
            }

            // Delete from S3
            await s3Service.DeleteObjectAsync(media.S3Key);

            // Delete from database
            db.Remove(media);
            await db.SaveChangesAsync();

            logger.LogInformation("Deleted media {MediaId} from anonymous gallery {GalleryId}", mediaId, gallery.Id);

            return Ok(new SuccessResponse("success"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting media from anonymous gallery");
            return Error(500, "failed_to_delete_media");
        }
    }
}
