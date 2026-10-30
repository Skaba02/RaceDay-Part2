using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Register, log in and log out. Everything is done by calling the API.</summary>
public class AccountController : BaseController
{
    public AccountController(ApiClient api) : base(api) { }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PostAsync<AuthResponse>("api/auth/register", new
        {
            fullName = model.FullName.Trim(),
            email = model.Email.Trim(),
            password = model.Password,
            role = model.Role
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We could not create your account. Please try again.");
            return View(model);
        }

        Success("Your account has been created. Please log in.");
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetInt32(SessionKeys.UserId) != null) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Start from a clean session so nothing from an earlier user is kept.
        HttpContext.Session.Clear();

        var result = await Api.PostAsync<AuthResponse>("api/auth/login", new
        {
            email = model.Email.Trim(),
            password = model.Password
        });

        if (!result.Success || result.Data == null)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We could not log you in. Please try again.");
            model.Password = string.Empty;
            return View(model);
        }

        // Remember who is logged in and what role they have, for every page.
        var user = result.Data;
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.FullName, user.FullName);
        HttpContext.Session.SetString(SessionKeys.Email, user.Email);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role);

        var profile = await Api.GetAsync<ProfileInfo>("api/me/profile");
        var hasPicture = profile.Success && profile.Data != null && profile.Data.HasProfilePicture;
        HttpContext.Session.SetString(SessionKeys.HasPicture, hasPicture ? "1" : "0");
        HttpContext.Session.SetString(SessionKeys.PictureVersion, DateTime.UtcNow.Ticks.ToString());

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        // Tell the API too, then end the website session.
        await Api.PostAsync("api/auth/logout");
        HttpContext.Session.Clear();

        Success("You have been logged out.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}
