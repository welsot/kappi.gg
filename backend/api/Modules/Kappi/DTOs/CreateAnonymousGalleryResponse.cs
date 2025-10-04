namespace api.Modules.Kappi.DTOs;

public record CreateAnonymousGalleryResponse(
    Guid GalleryId,
    string ShortCode,
    string AccessKey,
    DateTime ExpiresAt
);
