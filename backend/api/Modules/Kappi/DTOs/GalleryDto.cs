namespace api.Modules.Kappi.DTOs;

public record MediaListResponse(
    List<MediaDto> Media,
    int TotalCount
);

public record GalleryDto(
    Guid Id,
    string ShortCode,
    bool IsPublic,
    bool HasPassword,
    DateTime CreatedAt,
    MediaListResponse Media
);
