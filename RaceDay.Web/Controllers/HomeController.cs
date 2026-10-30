using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Helpers;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class HomeController : BaseController
{
    public HomeController(ApiClient api) : base(api) { }

    /// <summary>The landing page. Logged-in users go straight to their own home page.</summary>
    public IActionResult Index()
    {
        var role = HttpContext.Session.GetString(SessionKeys.Role);

        if (role == "Organiser") return RedirectToAction("Dashboard", "Organiser");
        if (role == "Participant") return RedirectToAction("Index", "Events");

        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        return View();
    }

    /// <summary>Friendly page for 404 and similar status codes.</summary>
    public IActionResult Status(int code)
    {
        ViewBag.Code = code;
        ViewBag.Heading = code == 404 ? "We could not find that page" : "Something went wrong";
        return View();
    }
}
