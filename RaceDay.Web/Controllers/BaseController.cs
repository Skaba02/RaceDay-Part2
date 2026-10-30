using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Shared helpers for every controller.</summary>
public abstract class BaseController : Controller
{
    protected readonly ApiClient Api;

    protected BaseController(ApiClient api)
    {
        Api = api;
    }

    /// <summary>
    /// If the API says the user is no longer logged in (for example it was restarted),
    /// clear the website session and send the user to the login page.
    /// Returns null when everything is fine.
    /// </summary>
    protected IActionResult? SessionExpired(ApiResult result)
    {
        if (!result.IsUnauthorized) return null;

        HttpContext.Session.Clear();
        TempData["Error"] = "Your session has ended. Please log in again.";
        return RedirectToAction("Login", "Account");
    }

    protected void Success(string message) => TempData["Success"] = message;

    protected void Fail(string? message) => TempData["Error"] = message ?? "Something went wrong. Please try again.";

    protected void Warn(string message) => TempData["Warning"] = message;
}
