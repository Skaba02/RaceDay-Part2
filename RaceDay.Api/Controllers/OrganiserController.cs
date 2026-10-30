using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Part3;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Organiser-only endpoints for the website: dashboard, enrolments per event, enrolment status and event banners.</summary>
[ApiController]
[Route("api/organiser")]
public class OrganiserController : ControllerBase
{
    private static readonly string[] AllowedStatuses = { "Pending", "Confirmed" };

    private readonly RaceDayDbContext _context;
    private readonly BlobStorageService _blobs;

    public OrganiserController(RaceDayDbContext context, IConfiguration configuration)
    {
        _context = context;
        _blobs = new BlobStorageService(configuration);
    }

    /// <summary>Summary of every event the logged-in Organiser created, with enrolment totals.</summary>
    [HttpGet("dashboard")]
    [RequireRole("Organiser")]
    public async Task<ActionResult<OrganiserDashboardDto>> GetDashboard()
    {
        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var events = await _context.Events
            .AsNoTracking()
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new OrganiserEventSummaryDto
            {
                EventId = e.EventId,
                Title = e.Title,
                EventDate = e.EventDate,
                Location = e.Location,
                EventType = e.EventType,
                CategoryCount = e.Categories.Count,
                TotalEnrolments = e.Categories.SelectMany(c => c.Enrolments).Count(),
                HasBanner = _context.EventImages.Any(i => i.EventId == e.EventId)
            })
            .ToListAsync();

        var today = DateTime.UtcNow.Date;

        return Ok(new OrganiserDashboardDto
        {
            TotalEvents = events.Count,
            TotalEnrolments = events.Sum(e => e.TotalEnrolments),
            NextEventDate = events.Where(e => e.EventDate >= today).Select(e => (DateTime?)e.EventDate).Min(),
            Events = events
        });
    }

    /// <summary>Everyone enrolled in one event, with their category, status and result. Must own the event.</summary>
    [HttpGet("events/{eventId:int}/enrolments")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var ev = await _context.Events.AsNoTracking().FirstOrDefaultAsync(e => e.EventId == eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var rows = await _context.Enrolments
            .AsNoTracking()
            .Where(en => en.Category!.EventId == eventId)
            .OrderBy(en => en.Category!.CategoryName)
            .ThenBy(en => en.Participant!.FullName)
            .Select(en => new EnrolmentRowDto
            {
                EnrolmentId = en.EnrolmentId,
                ParticipantName = en.Participant!.FullName,
                ParticipantEmail = en.Participant.Email,
                CategoryId = en.CategoryId,
                CategoryName = en.Category!.CategoryName,
                Status = en.Status,
                EnrolmentDate = en.EnrolmentDate,
                HasResult = en.Result != null,
                FinishTime = en.Result != null ? en.Result.FinishTime : null,
                Position = en.Result != null ? en.Result.Position : null
            })
            .ToListAsync();

        return Ok(rows);
    }

    /// <summary>Changes an enrolment status to Pending or Confirmed. Must own the event.</summary>
    [HttpPut("enrolments/{enrolmentId:int}/status")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> UpdateEnrolmentStatus(int enrolmentId, EnrolmentStatusDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var status = AllowedStatuses.FirstOrDefault(s => string.Equals(s, dto.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status == null)
            return BadRequest(new { message = "Status must be either 'Pending' or 'Confirmed'." });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var enrolment = await _context.Enrolments
            .Include(en => en.Category!)
            .ThenInclude(c => c!.Event)
            .FirstOrDefaultAsync(en => en.EnrolmentId == enrolmentId);

        if (enrolment == null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Category!.Event!.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        enrolment.Status = status;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Status updated.", status });
    }

    /// <summary>Uploads or replaces the banner image of an event (JPG, PNG or WEBP, up to 5 MB). Must own the event.</summary>
    [HttpPost("events/{eventId:int}/banner")]
    [RequireRole("Organiser")]
    public async Task<IActionResult> UploadBanner(int eventId, IFormFile? file)
    {
        var problem = ImageRules.Check(file);
        if (problem != null) return BadRequest(new { message = problem });

        var organiserId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var ev = await _context.Events.FindAsync(eventId);
        if (ev == null) return NotFound(new { message = "Event not found." });
        if (ev.OrganiserId != organiserId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not own this event." });

        var blobName = $"event-{eventId}-{Guid.NewGuid():N}{ImageRules.ExtensionFor(file!.ContentType)}";

        try
        {
            await using (var stream = file.OpenReadStream())
            {
                await _blobs.UploadAsync(BlobStorageService.EventBannerContainer, blobName, stream, file.ContentType);
            }

            var existing = await _context.EventImages.FindAsync(eventId);
            string? oldBlobName = null;

            if (existing == null)
            {
                _context.EventImages.Add(new EventImage { EventId = eventId, BlobName = blobName, ContentType = file.ContentType });
            }
            else
            {
                oldBlobName = existing.BlobName;
                existing.BlobName = blobName;
                existing.ContentType = file.ContentType;
                existing.UploadedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            if (oldBlobName != null)
            {
                await _blobs.DeleteAsync(BlobStorageService.EventBannerContainer, oldBlobName);
            }

            return Ok(new { message = "Banner uploaded." });
        }
        catch (BlobStorageNotConfiguredException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not set up yet." });
        }
        catch (Azure.RequestFailedException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Image storage is not available right now. Please try again later." });
        }
    }
}
