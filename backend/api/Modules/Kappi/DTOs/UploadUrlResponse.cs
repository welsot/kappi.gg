namespace api.Modules.Kappi.DTOs;

public record UploadUrlResponse(
    Guid MediaId,
    string UploadUrl,
    string S3Key
);
