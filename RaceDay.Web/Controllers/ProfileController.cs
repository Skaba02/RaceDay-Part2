using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Profile page for both roles: view and update details, upload a profile picture.</summary>
[RequireLogin]
public class ProfileController : BaseController
{
    public ProfileController(ApiClient api) : base(api) { }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var result = await Api.GetAsync<ProfileInfo>("api/me/profile");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success || result.Data == null)
        {
            Fail(result.Message);
            return RedirectToAction("Index", "Home");
        }

        return View(new ProfileViewModel
        {
            FullName = result.Data.FullName,
            Email = result.Data.Email,
            Role = result.Data.Role,
            HasProfilePicture = result.Data.HasProfilePicture
        });
    }

    [HttpPost]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Role = HttpContext.Session.GetString(SessionKeys.Role);
            model.HasProfilePicture = HttpContext.Session.GetString(SessionKeys.HasPicture) == "1";
            return View("Index", model);
        }

        var result = await Api.PutAsync("api/users/me", new
        {
            fullName = model.FullName.Trim(),
            email = model.Email.Trim()
        });

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success)
        {
            Fail(result.Message);
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetString(SessionKeys.FullName, model.FullName.Trim());
        HttpContext.Session.SetString(SessionKeys.Email, model.Email.Trim());

        Success("Your details have been updated.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> UploadPicture(IFormFile? picture)
    {
        if (picture == null || picture.Length == 0)
        {
            Fail("Please choose a picture first.");
            return RedirectToAction(nameof(Index));
        }

        var result = await Api.PostFileAsync("api/me/profile-picture", picture);

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success)
        {
            Fail(result.Message);
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetString(SessionKeys.HasPicture, "1");
        HttpContext.Session.SetString(SessionKeys.PictureVersion, DateTime.UtcNow.Ticks.ToString());

        Success("Your profile picture has been updated.");
        return RedirectToAction(nameof(Index));
    }
}
