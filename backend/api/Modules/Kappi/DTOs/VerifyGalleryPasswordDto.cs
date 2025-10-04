using System.ComponentModel.DataAnnotations;

namespace api.Modules.Kappi.DTOs;

public record VerifyGalleryPasswordDto(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Password
);
