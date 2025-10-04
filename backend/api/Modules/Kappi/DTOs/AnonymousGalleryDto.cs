namespace api.Modules.Kappi.DTOs;

public record AnonymousGalleryDto(
    Guid Id,
    string ShortCode,
    DateTime ExpiresAt,
    DateTime CreatedAt,
    MediaListResponse Media
);
