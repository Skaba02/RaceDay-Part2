using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant pages about the participant themselves: enrolments and race history.</summary>
[RequireRole("Participant")]
public class ParticipantController : BaseController
{
    public ParticipantController(ApiClient api) : base(api) { }

    public async Task<IActionResult> MyEnrolments()
    {
        var result = await Api.GetAsync<List<MyEnrolment>>("api/me/enrolments");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success) Fail(result.Message);

        return View(result.Data ?? new List<MyEnrolment>());
    }

    [HttpPost]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var result = await Api.DeleteAsync($"api/enrolments/{id}");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (result.Success) Success("Your enrolment has been cancelled.");
        else Fail(result.Message);

        return RedirectToAction(nameof(MyEnrolments));
    }

    public async Task<IActionResult> MyResults()
    {
        var result = await Api.GetAsync<List<MyResult>>("api/me/results");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success) Fail(result.Message);

        return View(result.Data ?? new List<MyResult>());
    }
}
