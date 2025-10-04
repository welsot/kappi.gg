using api.Modules.Common.Controllers;
using api.Modules.Common.Data;
using api.Modules.Common.DTO;
using api.Modules.Kappi.DTOs;
using api.Modules.Kappi.Models;
using api.Modules.Kappi.Repository;
using api.Modules.Kappi.Services;
using api.Modules.Storage.Services;
using api.Modules.User.Auth;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace api.Modules.Kappi.Controllers;

[ApiController]
[ApiTokenRequired]
public class GalleryController(
    Db db,
    ILogger<GalleryController> logger,
    IGalleryRepository galleries,
    IMediaRepository mediaRepository,
    ShortCodeGenerator shortCodeGenerator,
    MediaMetadataService metadataService,
    IS3Service s3Service,
    IPasswordHasher<Gallery> passwordHasher
) : ApiController
{
    [HttpPost("api/galleries")]
    [ProducesResponseType(typeof(GalleryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateGallery([FromBody] CreateGalleryDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var shortCode = await shortCodeGenerator.GenerateUniqueShortCodeAsync();
            var gallery = new Gallery(Guid.NewGuid(), userId, shortCode)
            {
                IsPublic = dto.IsPublic
            };

            // Set password if provided
            if (!string.IsNullOrEmpty(dto.Password))
            {
                var passwordHash = passwordHasher.HashPassword(gallery, dto.Password);
                gallery.SetPassword(passwordHash);
            }

            galleries.Add(gallery);
            await db.SaveChangesAsync();

            logger.LogInformation("Created gallery {GalleryId} for user {UserId}", gallery.Id, userId);

            return Created(ToGalleryDto(gallery, new List<MediaDto>()));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating gallery");
            return Error(500, "failed_to_create_gallery");
        }
    }

    [HttpGet("api/galleries")]
    [ProducesResponseType(typeof(GalleryListResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMyGalleries()
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var userGalleries = await galleries.GetByUserIdAsync(userId);

            var galleryDtos = new List<GalleryDto>();
            foreach (var gallery in userGalleries)
            {
                var mediaDtos = await GetMediaDtosAsync(gallery.Media);
                galleryDtos.Add(ToGalleryDto(gallery, mediaDtos));
            }

            logger.LogInformation("Retrieved {Count} galleries for user {UserId}", galleryDtos.Count, userId);

            return Ok(new GalleryListResponse(galleryDtos, galleryDtos.Count));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting user galleries");
            return Error(500, "failed_to_get_galleries");
        }
    }

    [HttpGet("api/galleries/{id}")]
    [ProducesResponseType(typeof(GalleryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetGallery(Guid id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var gallery = await galleries.FindByIdAndUserIdAsync(id, userId);
            if (gallery == null)
            {
                logger.LogWarning("Gallery {GalleryId} not found for user {UserId}", id, userId);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            var mediaDtos = await GetMediaDtosAsync(gallery.Media);

            logger.LogInformation("Retrieved gallery {GalleryId} for user {UserId}", id, userId);

            return Ok(ToGalleryDto(gallery, mediaDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting gallery {GalleryId}", id);
            return Error(500, "failed_to_get_gallery");
        }
    }

    [HttpPut("api/galleries/{id}")]
    [ProducesResponseType(typeof(GalleryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateGallery(Guid id, [FromBody] UpdateGalleryDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var gallery = await galleries.FindByIdAndUserIdAsync(id, userId);
            if (gallery == null)
            {
                logger.LogWarning("Gallery {GalleryId} not found for user {UserId}", id, userId);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            gallery.IsPublic = dto.IsPublic;

            // Update password
            if (!string.IsNullOrEmpty(dto.Password))
            {
                var passwordHash = passwordHasher.HashPassword(gallery, dto.Password);
                gallery.SetPassword(passwordHash);
            }
            else
            {
                gallery.ClearPassword();
            }

            await db.SaveChangesAsync();

            var mediaDtos = await GetMediaDtosAsync(gallery.Media);

            logger.LogInformation("Updated gallery {GalleryId} for user {UserId}", id, userId);

            return Ok(ToGalleryDto(gallery, mediaDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating gallery {GalleryId}", id);
            return Error(500, "failed_to_update_gallery");
        }
    }

    [HttpDelete("api/galleries/{id}")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteGallery(Guid id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var gallery = await galleries.FindByIdAndUserIdAsync(id, userId);
            if (gallery == null)
            {
                logger.LogWarning("Gallery {GalleryId} not found for user {UserId}", id, userId);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            // Delete all media from S3
            var s3Keys = gallery.Media.Select(m => m.S3Key).ToList();
            if (s3Keys.Any())
            {
                await s3Service.DeleteObjectsAsync(s3Keys);
            }

            // Delete gallery (cascade deletes media records)
            db.Remove(gallery);
            await db.SaveChangesAsync();

            logger.LogInformation("Deleted gallery {GalleryId} for user {UserId}", id, userId);

            return Ok(new SuccessResponse("success"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting gallery {GalleryId}", id);
            return Error(500, "failed_to_delete_gallery");
        }
    }

    [HttpPost("api/galleries/{id}/media/request-upload")]
    [ProducesResponseType(typeof(UploadUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestUploadUrl(Guid id, [FromBody] RequestUploadUrlDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var gallery = await galleries.FindByIdAndUserIdAsync(id, userId);
            if (gallery == null)
            {
                logger.LogWarning("Gallery {GalleryId} not found for user {UserId}", id, userId);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            // Generate S3 key
            var s3Key = $"galleries/user/{userId}/{gallery.Id}/{Guid.NewGuid()}/{dto.FileName}";

            // Create media record
            var media = new Media(Guid.NewGuid(), gallery.Id, s3Key, true);
            mediaRepository.Add(media);
            await db.SaveChangesAsync();

            // Generate pre-signed upload URL
            var uploadUrl = await s3Service.GeneratePresignedUploadUrlAsync(s3Key, dto.ContentType);

            logger.LogInformation("Generated upload URL for media {MediaId} in gallery {GalleryId}", media.Id, gallery.Id);

            return Ok(new UploadUrlResponse(media.Id, uploadUrl, s3Key));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error requesting upload URL for gallery {GalleryId}", id);
            return Error(500, "failed_to_generate_upload_url");
        }
    }

    [HttpPost("api/galleries/{id}/media/confirm-upload")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmUpload(Guid id, [FromBody] ConfirmUploadDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var gallery = await galleries.FindByIdAndUserIdAsync(id, userId);
            if (gallery == null)
            {
                logger.LogWarning("Gallery {GalleryId} not found for user {UserId}", id, userId);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            var media = await mediaRepository.FindByIdAsync(dto.MediaId);
            if (media == null || media.GalleryId != gallery.Id)
            {
                logger.LogWarning("Media {MediaId} not found in gallery {GalleryId}", dto.MediaId, gallery.Id);
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

            logger.LogInformation("Confirmed upload for media {MediaId} in gallery {GalleryId}", media.Id, gallery.Id);

            return Ok(new SuccessResponse("success"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error confirming upload for gallery {GalleryId}", id);
            return Error(500, "failed_to_confirm_upload");
        }
    }

    [HttpDelete("api/galleries/{id}/media/{mediaId}")]
    [ProducesResponseType(typeof(SuccessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteMedia(Guid id, Guid mediaId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(new ErrorResponse("unauthorized"));
            }

            var gallery = await galleries.FindByIdAndUserIdAsync(id, userId);
            if (gallery == null)
            {
                logger.LogWarning("Gallery {GalleryId} not found for user {UserId}", id, userId);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            var media = await mediaRepository.FindByIdAsync(mediaId);
            if (media == null || media.GalleryId != gallery.Id)
            {
                logger.LogWarning("Media {MediaId} not found in gallery {GalleryId}", mediaId, gallery.Id);
                return NotFound(new ErrorResponse("media_not_found"));
            }

            // Delete from S3
            await s3Service.DeleteObjectAsync(media.S3Key);

            // Delete from database
            db.Remove(media);
            await db.SaveChangesAsync();

            logger.LogInformation("Deleted media {MediaId} from gallery {GalleryId}", mediaId, gallery.Id);

            return Ok(new SuccessResponse("success"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting media from gallery {GalleryId}", id);
            return Error(500, "failed_to_delete_media");
        }
    }

    [HttpGet("api/galleries/by-short-code/{shortCode}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GalleryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetByShortCode(string shortCode)
    {
        try
        {
            var gallery = await galleries.FindByShortCodeAsync(shortCode);
            if (gallery == null)
            {
                logger.LogWarning("Gallery not found with short code {ShortCode}", shortCode);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            // Check if gallery is public
            if (!gallery.IsPublic)
            {
                logger.LogWarning("Gallery {GalleryId} is not public", gallery.Id);
                return Unauthorized(new ErrorResponse("gallery_not_public"));
            }

            // Check if password is required
            if (!string.IsNullOrEmpty(gallery.PasswordHash))
            {
                logger.LogWarning("Gallery {GalleryId} requires password", gallery.Id);
                return Unauthorized(new ErrorResponse("password_required"));
            }

            var mediaDtos = await GetMediaDtosAsync(gallery.Media);

            logger.LogInformation("Retrieved gallery {GalleryId} by short code {ShortCode}", gallery.Id, shortCode);

            return Ok(ToGalleryDto(gallery, mediaDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting gallery by short code {ShortCode}", shortCode);
            return Error(500, "failed_to_get_gallery");
        }
    }

    [HttpPost("api/galleries/by-short-code/{shortCode}/verify-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(GalleryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyPassword(string shortCode, [FromBody] VerifyGalleryPasswordDto dto)
    {
        try
        {
            var gallery = await galleries.FindByShortCodeAsync(shortCode);
            if (gallery == null)
            {
                logger.LogWarning("Gallery not found with short code {ShortCode}", shortCode);
                return NotFound(new ErrorResponse("gallery_not_found"));
            }

            // Check if gallery is public
            if (!gallery.IsPublic)
            {
                logger.LogWarning("Gallery {GalleryId} is not public", gallery.Id);
                return Unauthorized(new ErrorResponse("gallery_not_public"));
            }

            // Check password
            if (string.IsNullOrEmpty(gallery.PasswordHash))
            {
                logger.LogWarning("Gallery {GalleryId} does not have a password", gallery.Id);
                return Unauthorized(new ErrorResponse("password_not_required"));
            }

            var result = passwordHasher.VerifyHashedPassword(gallery, gallery.PasswordHash, dto.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                logger.LogWarning("Invalid password for gallery {GalleryId}", gallery.Id);
                return Unauthorized(new ErrorResponse("invalid_password"));
            }

            var mediaDtos = await GetMediaDtosAsync(gallery.Media);

            logger.LogInformation("Verified password for gallery {GalleryId} by short code {ShortCode}", gallery.Id, shortCode);

            return Ok(ToGalleryDto(gallery, mediaDtos));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying password for gallery by short code {ShortCode}", shortCode);
            return Error(500, "failed_to_verify_password");
        }
    }

    private async Task<List<MediaDto>> GetMediaDtosAsync(ICollection<Media> media)
    {
        var mediaDtos = new List<MediaDto>();
        foreach (var m in media)
        {
            // Fetch metadata if not present
            if (m.MediaType == null)
            {
                await metadataService.FetchAndStoreMetadataAsync(m);
                await db.SaveChangesAsync();
            }

            var downloadUrl = await s3Service.GeneratePresignedDownloadUrlAsync(m.S3Key);
            mediaDtos.Add(new MediaDto(
                m.Id,
                m.MediaType,
                m.Width,
                m.Height,
                m.FileSize,
                downloadUrl,
                m.CreatedAt
            ));
        }
        return mediaDtos;
    }

    private static GalleryDto ToGalleryDto(Gallery gallery, List<MediaDto> mediaDtos)
    {
        return new GalleryDto(
            gallery.Id,
            gallery.ShortCode,
            gallery.IsPublic,
            !string.IsNullOrEmpty(gallery.PasswordHash),
            gallery.CreatedAt,
            new MediaListResponse(mediaDtos, mediaDtos.Count)
        );
    }
}
