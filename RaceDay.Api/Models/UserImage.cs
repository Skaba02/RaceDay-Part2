namespace RaceDay.Api.Models;

/// <summary>Points a user at their profile picture stored in Blob Storage.</summary>
public class UserImage
{
    public int UserId { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
