using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

namespace api.Modules.Kappi.Models;

[Table("media")]
[Index(nameof(AnonymousGalleryId))]
[Index(nameof(GalleryId))]
[Index(nameof(S3Key), IsUnique = true)]
public class Media
{
    [Key]
    [Column]
    public Guid Id { get; private set; }

    [Column]
    public Guid? AnonymousGalleryId { get; private set; }

    [Column]
    public Guid? GalleryId { get; private set; }

    [StringLength(500)]
    [Column]
    public string S3Key { get; private set; } = string.Empty;

    [StringLength(50)]
    [Column]
    public string? MediaType { get; set; }

    [Column]
    public int? Width { get; set; }

    [Column]
    public int? Height { get; set; }

    [Column]
    public long? FileSize { get; set; }

    [Column]
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    [ForeignKey(nameof(AnonymousGalleryId))]
    public virtual AnonymousGallery? AnonymousGallery { get; private set; }

    [ForeignKey(nameof(GalleryId))]
    public virtual Gallery? Gallery { get; private set; }

    private Media() { }

    public Media(Guid id, Guid anonymousGalleryId, string s3Key)
    {
        Id = id;
        AnonymousGalleryId = anonymousGalleryId;
        S3Key = s3Key;
    }

    public Media(Guid id, Guid galleryId, string s3Key, bool _)
    {
        Id = id;
        GalleryId = galleryId;
        S3Key = s3Key;
    }

    public void SetMetadata(string? mediaType, int? width, int? height, long? fileSize)
    {
        MediaType = mediaType;
        Width = width;
        Height = height;
        FileSize = fileSize;
    }
}
