using System.ComponentModel.DataAnnotations;

namespace api.Modules.User.DTOs;

public class TokenRefreshDto
{
    [Required]
    [MinLength(1)]
    public string RefreshToken { get; set; } = string.Empty;
}
