using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Events;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Event management. Viewing is public; creating, updating, and deleting are Organiser-only.</summary>
[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Lists all events. Public - no login required.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventResponseDto>>> GetEvents()
    {
        var events = await _context.Events.ToListAsync();
        return Ok(events.Select(ToDto));
    }

    /// <summary>Returns full details for one event. Public - no login required.</summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<EventResponseDto>> GetEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });

        return Ok(ToDto(ev));
    }

    /// <summary>Creates a new event. Organiser only.</summary>
    [HttpPost]
    [RequireRole("Organiser")]
    public async Task<ActionResult<EventResponseDto>> CreateEvent(EventCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var ev = new Event
        {
            OrganiserId = organiserId,
            Title = dto.Title.Trim(),
            Description = dto.Description,
            EventDate = dto.EventDate,
            Location = dto.Location.Trim(),
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType.Trim()
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId }, ToDto(ev));
    }

    /// <summary>Updates an event owned by the logged-in Organiser.</summary>
    [HttpPut("{id}")]
    [RequireRole("Organiser")]
    public async Task<ActionResult<EventResponseDto>> UpdateEvent(int id, EventUpdateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        ev.Title = dto.Title.Trim();
        ev.Description = dto.Description;
        ev.EventDate = dto.EventDate;
        ev.Location = dto.Location.Trim();
        ev.DistanceKm = dto.DistanceKm;
        ev.EventType = dto.EventType.Trim();

        await _context.SaveChangesAsync();
        return Ok(ToDto(ev));
    }

    /// <summary>Deletes an event owned by the logged-in Organiser.</summary>
    [HttpDelete("{id}")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var ev = await _context.Events.FindAsync(id);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static EventResponseDto ToDto(Event ev) => new()
    {
        EventId = ev.EventId,
        OrganiserId = ev.OrganiserId,
        Title = ev.Title,
        Description = ev.Description,
        EventDate = ev.EventDate,
        Location = ev.Location,
        DistanceKm = ev.DistanceKm,
        EventType = ev.EventType
    };
}
