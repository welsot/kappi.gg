using System.ComponentModel.DataAnnotations;

namespace api.Modules.Kappi.DTOs;

public record UpdateGalleryDto(
    [Required]
    bool IsPublic,

    [StringLength(100, MinimumLength = 8)]
    string? Password
);
