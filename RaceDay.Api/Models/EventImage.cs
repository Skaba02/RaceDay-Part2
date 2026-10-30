namespace RaceDay.Api.Models;

/// <summary>Points an event at its banner image stored in Blob Storage.</summary>
public class EventImage
{
    public int EventId { get; set; }
    public string BlobName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
