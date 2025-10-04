using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

using UserModel = api.Modules.User.Models.User;

namespace api.Modules.Kappi.Models;

[Table("galleries")]
[Index(nameof(ShortCode), IsUnique = true)]
[Index(nameof(UserId))]
public class Gallery
{
    [Key]
    [Column]
    public Guid Id { get; private set; }

    [Column]
    public Guid UserId { get; private set; }

    [StringLength(10)]
    [Column]
    public string ShortCode { get; set; } = string.Empty;

    [Column]
    public bool IsPublic { get; set; } = false;

    [StringLength(255)]
    [Column]
    public string? PasswordHash { get; set; }

    [Column]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public virtual UserModel User { get; private set; } = null!;

    public virtual ICollection<Media> Media { get; private set; } = new List<Media>();

    private Gallery() { }

    public Gallery(Guid id, Guid userId, string shortCode)
    {
        Id = id;
        UserId = userId;
        ShortCode = shortCode;
    }

    public void SetPassword(string passwordHash)
    {
        PasswordHash = passwordHash;
    }

    public void ClearPassword()
    {
        PasswordHash = null;
    }
}
