using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace api.Modules.Kappi.Models;

[Table("anonymous_galleries")]
[Index(nameof(ShortCode), IsUnique = true)]
[Index(nameof(AccessKey), IsUnique = true)]
[Index(nameof(ExpiresAt))]
public class AnonymousGallery
{
    [Key]
    [Column]
    public Guid Id { get; private set; }

    [StringLength(10)]
    [Column]
    public string ShortCode { get; private set; } = string.Empty;

    [StringLength(64)]
    [Column]
    public string AccessKey { get; private set; } = string.Empty;

    [Column]
    public DateTime ExpiresAt { get; private set; }

    [Column]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public virtual ICollection<Media> Media { get; private set; } = new List<Media>();

    private AnonymousGallery() { }

    public AnonymousGallery(Guid id, string shortCode, string accessKey)
    {
        Id = id;
        ShortCode = shortCode;
        AccessKey = accessKey;
        ExpiresAt = DateTime.UtcNow.AddDays(30);
    }
}
