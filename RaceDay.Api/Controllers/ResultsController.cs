using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Results;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Recording and viewing race results. Recording is Organiser-only; viewing is public or restricted to the owning Participant.</summary>
[ApiController]
[Route("api")]
public class ResultsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Records a finish time and position for an enrolment. Organiser only, and must own the event.</summary>
    [HttpPost("enrolments/{enrolmentId}/result")]
    [RequireRole("Organiser")]
    public async Task<ActionResult<ResultResponseDto>> CreateResult(int enrolmentId, ResultCreateDto dto)
    {
        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var enrolment = await _context.Enrolments
            .Include(en => en.Category!)
            .ThenInclude(c => c!.Event)
            .Include(en => en.Result)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Category!.Event!.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });
        if (enrolment.Result != null)
            return Conflict(new { message = "A result has already been recorded for this enrolment." });

        var result = new Result
        {
            EnrolmentId = enrolmentId,
            FinishTime = dto.FinishTime,
            Position = dto.Position
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCategoryResults), new { categoryId = enrolment.CategoryId }, ToDto(result));
    }

    /// <summary>Lists all results for a category, ordered by position. Public.</summary>
    [HttpGet("categories/{categoryId}/results")]
    public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetCategoryResults(int categoryId)
    {
        var categoryExists = await _context.Categories.AnyAsync(c => c.CategoryId == categoryId);
        if (!categoryExists) return NotFound(new { message = "Category not found." });

        var results = await _context.Results
            .Include(r => r.Enrolment)
            .Where(r => r.Enrolment!.CategoryId == categoryId)
            .OrderBy(r => r.Position)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }

    /// <summary>Lists all results for the logged-in Participant.</summary>
    [HttpGet("users/me/results")]
    [RequireRole("Participant")]
    public async Task<ActionResult<IEnumerable<ResultResponseDto>>> GetMyResults()
    {
        var participantId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var results = await _context.Results
            .Include(r => r.Enrolment)
            .Where(r => r.Enrolment!.ParticipantId == participantId)
            .ToListAsync();

        return Ok(results.Select(ToDto));
    }

    private static ResultResponseDto ToDto(Result r) => new()
    {
        ResultId = r.ResultId,
        EnrolmentId = r.EnrolmentId,
        FinishTime = r.FinishTime,
        Position = r.Position
    };
}
