using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>
/// Shows images on the pages. The website asks the API for the file and passes it on,
/// so the website never talks to Azure Blob Storage itself.
/// </summary>
public class ImagesController : BaseController
{
    public ImagesController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Event(int id)
    {
        var file = await Api.GetFileAsync($"api/images/events/{id}");
        if (file == null) return NotFound();

        Response.Headers.CacheControl = "private, max-age=60";
        return File(file.Bytes, file.ContentType);
    }

    [HttpGet]
    [RequireLogin]
    public async Task<IActionResult> Profile()
    {
        var file = await Api.GetFileAsync("api/me/profile-picture");
        if (file == null) return NotFound();

        Response.Headers.CacheControl = "private, max-age=60";
        return File(file.Bytes, file.ContentType);
    }
}
