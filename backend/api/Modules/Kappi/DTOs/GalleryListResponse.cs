namespace api.Modules.Kappi.DTOs;

public record GalleryListResponse(
    List<GalleryDto> Galleries,
    int TotalCount
);
