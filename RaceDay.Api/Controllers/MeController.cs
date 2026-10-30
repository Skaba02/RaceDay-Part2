using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Part3;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Endpoints about the logged-in user: their enrolments, race history and profile picture.</summary>
[ApiController]
[Route("api/me")]
public class MeController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly BlobStorageService _blobs;

    public MeController(RaceDayDbContext context, IConfiguration configuration)
    {
        _context = context;
        _blobs = new BlobStorageService(configuration);
    }

    /// <summary>Every event the logged-in Participant has enrolled in, with the enrolment status.</summary>
    [HttpGet("enrolments")]
    [RequireRole("Participant")]
    public async Task<ActionResult<IEnumerable<MyEnrolmentDto>>> GetMyEnrolments()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var items = await _context.Enrolments
            .AsNoTracking()
            .Where(en => en.ParticipantId == userId)
            .OrderBy(en => en.Category!.Event!.EventDate)
            .Select(en => new MyEnrolmentDto
            {
                EnrolmentId = en.EnrolmentId,
                EventId = en.Category!.EventId,
                CategoryId = en.CategoryId,
                EventTitle = en.Category.Event!.Title,
                EventDate = en.Category.Event.EventDate,
                Location = en.Category.Event.Location,
                CategoryName = en.Category.CategoryName,
                Status = en.Status,
                EnrolmentDate = en.EnrolmentDate,
                HasResult = en.Result != null
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>The logged-in Participant's race history: event, date, category, finish time and position out of all finishers.</summary>
    [HttpGet("results")]
    [RequireRole("Participant")]
    public async Task<ActionResult<IEnumerable<MyResultDto>>> GetMyResults()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var mine = await _context.Results
            .AsNoTracking()
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .Select(r => new
            {
                r.ResultId,
                EventId = r.Enrolment!.Category!.EventId,
                EventTitle = r.Enrolment.Category.Event!.Title,
                EventDate = r.Enrolment.Category.Event.EventDate,
                CategoryId = r.Enrolment.CategoryId,
                CategoryName = r.Enrolment.Category.CategoryName,
                r.FinishTime,
                r.Position
            })
            .ToListAsync();

        var categoryIds = mine.Select(m => m.CategoryId).Distinct().ToList();

        var totals = await _context.Results
            .AsNoTracking()
            .Where(r => categoryIds.Contains(r.Enrolment!.CategoryId) && r.Position != null)
            .GroupBy(r => r.Enrolment!.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Count() })
            .ToListAsync();

        var items = mine
            .OrderByDescending(m => m.EventDate)
            .Select(m => new MyResultDto
            {
                ResultId = m.ResultId,
                EventId = m.EventId,
                EventTitle = m.EventTitle,
                EventDate = m.EventDate,
                CategoryName = m.CategoryName,
                FinishTime = m.FinishTime,
                Position = m.Position,
                TotalFinishers = totals.FirstOrDefault(t => t.CategoryId == m.CategoryId)?.Total ?? 0
            })
            .ToList();

        return Ok(items);
    }

    /// <summary>The logged-in user's profile (any role).</summary>
    [HttpGet("profile")]
    [RequireAuth]
    public async Task<ActionResult<ProfileDto>> GetProfile()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var profile = await _context.Users
            .AsNoTracking()
            .Where(u => u.UserId == userId)
            .Select(u => new ProfileDto
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role!.RoleName,
                HasProfilePicture = _context.UserImages.Any(i => i.UserId == u.UserId)
            })
            .FirstOrDefaultAsync();

        if (profile == null) return NotFound(new { message = "User not found." });

        return Ok(profile);
    }

    /// <summary>Uploads or replaces the logged-in user's profile picture (JPG, PNG or WEBP, up to 5 MB).</summary>
    [HttpPost("profile-picture")]
    [RequireAuth]
    public async Task<IActionResult> UploadProfilePicture(IFormFile? file)
    {
        var problem = ImageRules.Check(file);
        if (problem != null) return BadRequest(new { message = problem });

        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var blobName = $"user-{userId}-{Guid.NewGuid():N}{ImageRules.ExtensionFor(file!.ContentType)}";

        try
        {
            await using (var stream = file.OpenReadStream())
            {
                await _blobs.UploadAsync(BlobStorageService.ProfilePictureContainer, blobName, stream, file.ContentType);
            }

            var existing = await _context.UserImages.FindAsync(userId);
            string? oldBlobName = null;

            if (existing == null)
            {
                _context.UserImages.Add(new UserImage { UserId = userId, BlobName = blobName, ContentType = file.ContentType });
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
                await _blobs.DeleteAsync(BlobStorageService.ProfilePictureContainer, oldBlobName);
            }

            return Ok(new { message = "Profile picture uploaded." });
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

    /// <summary>Returns the logged-in user's profile picture file.</summary>
    [HttpGet("profile-picture")]
    [RequireAuth]
    public async Task<IActionResult> GetProfilePicture()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;

        var image = await _context.UserImages.AsNoTracking().FirstOrDefaultAsync(i => i.UserId == userId);
        if (image == null) return NotFound(new { message = "No profile picture yet." });

        try
        {
            var download = await _blobs.DownloadAsync(BlobStorageService.ProfilePictureContainer, image.BlobName);
            if (download == null) return NotFound(new { message = "No profile picture yet." });

            return File(download.Content, download.ContentType);
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
