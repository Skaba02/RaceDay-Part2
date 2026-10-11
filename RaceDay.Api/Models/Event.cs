namespace RaceDay.Api.Models;

public class Event
{
    public int EventId { get; set; }
    public int OrganiserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;

    // Added in Part 2: the functional requirements ask each event to capture
    // its own distance and type, in addition to the age/distance Categories
    // already defined in the Part 1 ERD.
    public decimal? DistanceKm { get; set; }

    /// <summary>Run, Walk, or Cycle.</summary>
    public string EventType { get; set; } = "Run";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? Organiser { get; set; }
    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
