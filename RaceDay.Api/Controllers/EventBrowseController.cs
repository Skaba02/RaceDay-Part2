using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Part3;

namespace RaceDay.Api.Controllers;

/// <summary>Public endpoints the website uses to browse events: search with filters, and full event details.</summary>
[ApiController]
[Route("api/events")]
public class EventBrowseController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventBrowseController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Searches events. Public. Every filter is optional. Only upcoming events are returned unless includePast is true.</summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<EventListItemDto>>> Search(
        string? q, string? eventType, string? location, DateTime? dateFrom, DateTime? dateTo, bool includePast = false)
    {
        var query = _context.Events.AsNoTracking().AsQueryable();

        if (!includePast)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(e => e.EventDate >= today);
        }

        if (!string.IsNullOrWhiteSpace(q))
        {
            var text = q.Trim();
            query = query.Where(e => e.Title.Contains(text) || (e.Description != null && e.Description.Contains(text)));
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = eventType.Trim();
            query = query.Where(e => e.EventType == type);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var place = location.Trim();
            query = query.Where(e => e.Location.Contains(place));
        }

        if (dateFrom.HasValue)
        {
            var start = dateFrom.Value.Date;
            query = query.Where(e => e.EventDate >= start);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1);
            query = query.Where(e => e.EventDate < end);
        }

        var items = await query
            .OrderBy(e => e.EventDate)
            .Select(e => new EventListItemDto
            {
                EventId = e.EventId,
                Title = e.Title,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                OrganiserName = e.Organiser!.FullName,
                CategoryCount = e.Categories.Count,
                HasBanner = _context.EventImages.Any(i => i.EventId == e.EventId)
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>Returns one event with its categories and how many people are enrolled in each. Public.</summary>
    [HttpGet("{id:int}/details")]
    public async Task<ActionResult<EventDetailsDto>> GetDetails(int id)
    {
        var details = await _context.Events
            .AsNoTracking()
            .Where(e => e.EventId == id)
            .Select(e => new EventDetailsDto
            {
                EventId = e.EventId,
                OrganiserId = e.OrganiserId,
                Title = e.Title,
                Description = e.Description,
                EventDate = e.EventDate,
                Location = e.Location,
                DistanceKm = e.DistanceKm,
                EventType = e.EventType,
                OrganiserName = e.Organiser!.FullName,
                CategoryCount = e.Categories.Count,
                HasBanner = _context.EventImages.Any(i => i.EventId == e.EventId),
                Categories = e.Categories
                    .OrderBy(c => c.DistanceKm)
                    .Select(c => new CategoryInfoDto
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        DistanceKm = c.DistanceKm,
                        MaxParticipants = c.MaxParticipants,
                        EnrolledCount = c.Enrolments.Count
                    })
                    .ToList()
            })
            .FirstOrDefaultAsync();

        if (details == null) return NotFound(new { message = "Event not found." });

        return Ok(details);
    }
}
