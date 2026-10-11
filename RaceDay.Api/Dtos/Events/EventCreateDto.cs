using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Events;

public class EventCreateDto
{
    [Required, MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required]
    public DateTime EventDate { get; set; }

    [Required, MaxLength(150)]
    public string Location { get; set; } = string.Empty;

    public decimal? DistanceKm { get; set; }

    /// <summary>Run, Walk, or Cycle.</summary>
    [Required]
    public string EventType { get; set; } = "Run";
}
