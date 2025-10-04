namespace api.Modules.Kappi.DTOs;

public record GalleryDto(
    Guid Id,
    string ShortCode,
    bool IsPublic,
    bool HasPassword,
    DateTime CreatedAt,
    List<MediaDto> Media
);
