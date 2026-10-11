using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Enrolments;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Enrolling in categories. Enrolling and viewing "my enrolments" are Participant-only; Organisers can view who is enrolled in their own categories.</summary>
[ApiController]
[Route("api")]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Enrols the logged-in Participant in a category.</summary>
    [HttpPost("categories/{categoryId}/enrol")]
    [RequireRole("Participant")]
    public async Task<ActionResult<EnrolmentResponseDto>> Enrol(int categoryId)
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null) return NotFound(new { message = "Category not found." });

        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(en => en.ParticipantId == participantId && en.CategoryId == categoryId);
        if (alreadyEnrolled)
            return Conflict(new { message = "You are already enrolled in this category." });

        var currentCount = await _context.Enrolments.CountAsync(en => en.CategoryId == categoryId);
        if (currentCount >= category.MaxParticipants)
            return Conflict(new { message = "This category is full." });

        var enrolment = new Enrolment
        {
            ParticipantId = participantId,
            CategoryId = categoryId
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var dto = await ToDtoAsync(enrolment.EnrolmentId);
        return CreatedAtAction(nameof(GetMyEnrolments), null, dto);
    }

    /// <summary>Lists all enrolments for the logged-in Participant.</summary>
    [HttpGet("users/me/enrolments")]
    [RequireRole("Participant")]
    public async Task<ActionResult<IEnumerable<EnrolmentResponseDto>>> GetMyEnrolments()
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var enrolments = await _context.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category)
            .Where(en => en.ParticipantId == participantId)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>Lists all participants enrolled in a category. Organiser only, and must own the event.</summary>
    [HttpGet("categories/{categoryId}/enrolments")]
    [RequireRole("Organiser")]
    public async Task<ActionResult<IEnumerable<EnrolmentResponseDto>>> GetCategoryEnrolments(int categoryId)
    {
        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var category = await _context.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category == null) return NotFound(new { message = "Category not found." });
        if (category.Event!.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var enrolments = await _context.Enrolments
            .Include(en => en.Participant)
            .Include(en => en.Category)
            .Where(en => en.CategoryId == categoryId)
            .ToListAsync();

        return Ok(enrolments.Select(ToDto));
    }

    /// <summary>Cancels an enrolment belonging to the logged-in Participant.</summary>
    [HttpDelete("enrolments/{id}")]
    [RequireRole("Participant")]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var enrolment = await _context.Enrolments.FindAsync(id);
        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.ParticipantId != participantId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "This is not your enrolment." });

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static EnrolmentResponseDto ToDto(Enrolment en) => new()
    {
        EnrolmentId = en.EnrolmentId,
        ParticipantId = en.ParticipantId,
        ParticipantName = en.Participant!.FullName,
        CategoryId = en.CategoryId,
        CategoryName = en.Category!.CategoryName,
        EnrolmentDate = en.EnrolmentDate,
        Status = en.Status
    };

    private async Task<EnrolmentResponseDto> ToDtoAsync(int enrolmentId)
    {
        var en = await _context.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .FirstAsync(e => e.EnrolmentId == enrolmentId);

        return ToDto(en);
    }
}
