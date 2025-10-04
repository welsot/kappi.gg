using System.ComponentModel.DataAnnotations;

namespace api.Modules.Kappi.DTOs;

public record ConfirmUploadDto(
    [Required]
    Guid MediaId
);
