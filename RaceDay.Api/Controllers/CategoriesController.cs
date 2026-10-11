using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Category management within an event. Viewing is public; writing is Organiser-only and must own the event.</summary>
[ApiController]
[Route("api")]
public class CategoriesController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public CategoriesController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Lists all categories for a specific event. Public.</summary>
    [HttpGet("events/{eventId}/categories")]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetCategories(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound(new { message = "Event not found." });

        var categories = await _context.Categories
            .Where(c => c.EventId == eventId)
            .ToListAsync();

        return Ok(categories.Select(ToDto));
    }

    /// <summary>Adds a new category to an event. Organiser only, and must own the event.</summary>
    [HttpPost("events/{eventId}/categories")]
    [RequireRole("Organiser")]
    public async Task<ActionResult<CategoryResponseDto>> CreateCategory(int eventId, CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var category = new Category
        {
            EventId = eventId,
            CategoryName = dto.CategoryName.Trim(),
            DistanceKm = dto.DistanceKm,
            MaxParticipants = dto.MaxParticipants
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategories), new { eventId }, ToDto(category));
    }

    /// <summary>Updates a category's details. Organiser only, and must own the event.</summary>
    [HttpPut("categories/{id}")]
    [RequireRole("Organiser")]
    public async Task<ActionResult<CategoryResponseDto>> UpdateCategory(int id, CategoryCreateDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category not found." });
        if (category.Event!.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        category.CategoryName = dto.CategoryName.Trim();
        category.DistanceKm = dto.DistanceKm;
        category.MaxParticipants = dto.MaxParticipants;

        await _context.SaveChangesAsync();
        return Ok(ToDto(category));
    }

    /// <summary>Removes a category from an event. Organiser only, and must own the event.</summary>
    [HttpDelete("categories/{id}")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var category = await _context.Categories.Include(c => c.Event).FirstOrDefaultAsync(c => c.CategoryId == id);
        if (category == null) return NotFound(new { message = "Category not found." });
        if (category.Event!.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static CategoryResponseDto ToDto(Category c) => new()
    {
        CategoryId = c.CategoryId,
        EventId = c.EventId,
        CategoryName = c.CategoryName,
        DistanceKm = c.DistanceKm,
        MaxParticipants = c.MaxParticipants
    };
}
