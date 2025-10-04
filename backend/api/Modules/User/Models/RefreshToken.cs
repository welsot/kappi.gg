using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using api.Modules.Common.Services;

using Microsoft.EntityFrameworkCore;

namespace api.Modules.User.Models;

[Table("refresh_token")]
[Index(nameof(Token), IsUnique = true)]
public class RefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; private set; }

    [Required]
    [StringLength(255)]
    [Column]
    public string Token { get; private set; }

    [Required]
    [ForeignKey("UserId")]
    public User User { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    [Required]
    public DateTime ExpiresAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    private RefreshToken()
    {
    }

    public RefreshToken(User user, int expirationDays = 60)
    {
        User = user;
        Token = RandomTokenGenerator.GenerateRandomToken();
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddDays(expirationDays);
    }

    public bool IsValid()
    {
        return RevokedAt == null && ExpiresAt > DateTime.UtcNow;
    }

    public void Revoke()
    {
        RevokedAt = DateTime.UtcNow;
    }
}
