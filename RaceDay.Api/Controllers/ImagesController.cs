using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

namespace RaceDay.Api.Controllers;

/// <summary>Serves event banner images from Blob Storage. Public, so the event pages can show them.</summary>
[ApiController]
[Route("api/images")]
public class ImagesController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly BlobStorageService _blobs;

    public ImagesController(RaceDayDbContext context, IConfiguration configuration)
    {
        _context = context;
        _blobs = new BlobStorageService(configuration);
    }

    /// <summary>Returns the banner image of an event.</summary>
    [HttpGet("events/{eventId:int}")]
    public async Task<IActionResult> GetEventBanner(int eventId)
    {
        var image = await _context.EventImages.AsNoTracking().FirstOrDefaultAsync(i => i.EventId == eventId);
        if (image == null) return NotFound(new { message = "This event has no banner." });

        try
        {
            var download = await _blobs.DownloadAsync(BlobStorageService.EventBannerContainer, image.BlobName);
            if (download == null) return NotFound(new { message = "This event has no banner." });

            Response.Headers.CacheControl = "public, max-age=300";
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
