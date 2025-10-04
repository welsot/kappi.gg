namespace api.Modules.Kappi.DTOs;

public record MediaDto(
    Guid Id,
    string? MediaType,
    int? Width,
    int? Height,
    long? FileSize,
    string DownloadUrl,
    DateTime CreatedAt
);
