namespace RaceDay.Api.Dtos.Events;

public class EventResponseDto
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
}
