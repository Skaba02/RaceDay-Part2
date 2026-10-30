using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Participant pages: browse upcoming events, see full details and enrol.</summary>
[RequireRole("Participant")]
public class EventsController : BaseController
{
    public EventsController(ApiClient api) : base(api) { }

    /// <summary>Participant home page: upcoming events with filters.</summary>
    public async Task<IActionResult> Index(EventFilter filter)
    {
        var parts = new List<string>();

        void Add(string name, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value)) parts.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
        }

        Add("q", filter.Q);
        Add("eventType", filter.EventType);
        Add("location", filter.Location);
        Add("dateFrom", filter.DateFrom?.ToString("yyyy-MM-dd"));
        Add("dateTo", filter.DateTo?.ToString("yyyy-MM-dd"));

        var url = "api/events/search" + (parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty);
        var result = await Api.GetAsync<List<EventListItem>>(url);

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success) Fail(result.Message);

        return View(new EventListViewModel { Filter = filter, Events = result.Data ?? new List<EventListItem>() });
    }

    /// <summary>Full information about one event and its categories.</summary>
    public async Task<IActionResult> Details(int id)
    {
        var details = await Api.GetAsync<EventDetails>($"api/events/{id}/details");

        var expired = SessionExpired(details);
        if (expired != null) return expired;

        if (!details.Success || details.Data == null)
        {
            Fail(details.Message ?? "We could not find that event.");
            return RedirectToAction(nameof(Index));
        }

        // Which categories has this participant already joined?
        var mine = await Api.GetAsync<List<MyEnrolment>>("api/me/enrolments");
        var enrolledIds = mine.Success && mine.Data != null
            ? mine.Data.Select(e => e.CategoryId).ToHashSet()
            : new HashSet<int>();

        return View(new EventDetailsViewModel { Event = details.Data, EnrolledCategoryIds = enrolledIds });
    }

    /// <summary>Enrol the logged-in participant in a category. The API records it immediately.</summary>
    [HttpPost]
    public async Task<IActionResult> Enrol(int eventId, int categoryId)
    {
        var result = await Api.PostAsync($"api/categories/{categoryId}/enrol");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success)
        {
            Fail(result.Message);
            return RedirectToAction(nameof(Details), new { id = eventId });
        }

        Success("You are enrolled. You can see it under My Enrolments.");
        return RedirectToAction("MyEnrolments", "Participant");
    }
}
