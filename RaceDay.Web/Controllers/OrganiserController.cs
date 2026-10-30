using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Models.Api;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>All Organiser pages: dashboard, events, categories, enrolments and results.</summary>
[RequireRole("Organiser")]
public class OrganiserController : BaseController
{
    public OrganiserController(ApiClient api) : base(api) { }

    // ---------------------------------------------------------------- Dashboard

    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetAsync<OrganiserDashboard>("api/organiser/dashboard");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success) Fail(result.Message);

        return View(result.Data ?? new OrganiserDashboard());
    }

    // ------------------------------------------------------------------- Events

    /// <summary>Events management page: the list with create, edit and delete.</summary>
    public async Task<IActionResult> Events()
    {
        var result = await Api.GetAsync<OrganiserDashboard>("api/organiser/dashboard");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success) Fail(result.Message);

        return View(result.Data ?? new OrganiserDashboard());
    }

    [HttpGet]
    public IActionResult CreateEvent()
    {
        return View("EventForm", new EventFormViewModel { EventDate = DateTime.Today.AddDays(30).AddHours(8) });
    }

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);

        var created = await Api.PostAsync<CreatedEvent>("api/events", EventBody(model));

        var expired = SessionExpired(created);
        if (expired != null) return expired;

        if (!created.Success || created.Data == null)
        {
            ModelState.AddModelError(string.Empty, created.Message ?? "We could not create the event. Please try again.");
            return View("EventForm", model);
        }

        // The banner image is optional. It goes to the API, which stores it in Blob Storage.
        if (model.Banner != null && model.Banner.Length > 0)
        {
            var upload = await Api.PostFileAsync($"api/organiser/events/{created.Data.EventId}/banner", model.Banner);

            if (!upload.Success)
            {
                Warn($"The event was created, but the banner could not be uploaded: {upload.Message} You can try again by editing the event.");
                return RedirectToAction(nameof(Events));
            }
        }

        Success("The event has been created. Next, add its categories.");
        return RedirectToAction(nameof(Categories), new { eventId = created.Data.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var details = await Api.GetAsync<EventDetails>($"api/events/{id}/details");

        var expired = SessionExpired(details);
        if (expired != null) return expired;

        if (!details.Success || details.Data == null)
        {
            Fail(details.Message);
            return RedirectToAction(nameof(Events));
        }

        var e = details.Data;
        return View("EventForm", new EventFormViewModel
        {
            EventId = e.EventId,
            Title = e.Title,
            Description = e.Description,
            EventDate = e.EventDate,
            Location = e.Location,
            DistanceKm = e.DistanceKm,
            EventType = e.EventType,
            HasBanner = e.HasBanner
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(EventFormViewModel model)
    {
        if (!ModelState.IsValid) return View("EventForm", model);

        var saved = await Api.PutAsync($"api/events/{model.EventId}", EventBody(model));

        var expired = SessionExpired(saved);
        if (expired != null) return expired;

        if (!saved.Success)
        {
            ModelState.AddModelError(string.Empty, saved.Message ?? "We could not save the event. Please try again.");
            return View("EventForm", model);
        }

        if (model.Banner != null && model.Banner.Length > 0)
        {
            var upload = await Api.PostFileAsync($"api/organiser/events/{model.EventId}/banner", model.Banner);

            if (!upload.Success)
            {
                Warn($"The event was saved, but the banner could not be uploaded: {upload.Message}");
                return RedirectToAction(nameof(Events));
            }
        }

        Success("The event has been updated.");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (result.Success) Success("The event has been deleted.");
        else Fail(result.Message);

        return RedirectToAction(nameof(Events));
    }

    // --------------------------------------------------------------- Categories

    public async Task<IActionResult> Categories(int eventId)
    {
        var details = await Api.GetAsync<EventDetails>($"api/events/{eventId}/details");

        var expired = SessionExpired(details);
        if (expired != null) return expired;

        if (!details.Success || details.Data == null)
        {
            Fail(details.Message);
            return RedirectToAction(nameof(Events));
        }

        return View(new CategoriesViewModel
        {
            Event = details.Data,
            NewCategory = new CategoryFormViewModel { EventId = eventId, DistanceKm = details.Data.DistanceKm ?? 5 }
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Fail(FirstError() ?? "Please check the category details.");
            return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
        }

        var result = await Api.PostAsync($"api/events/{model.EventId}/categories", new
        {
            categoryName = model.CategoryName.Trim(),
            distanceKm = model.DistanceKm,
            maxParticipants = model.MaxParticipants
        });

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (result.Success) Success("The category has been added.");
        else Fail(result.Message);

        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int eventId, int id)
    {
        var details = await Api.GetAsync<EventDetails>($"api/events/{eventId}/details");

        var expired = SessionExpired(details);
        if (expired != null) return expired;

        var category = details.Data?.Categories.FirstOrDefault(c => c.CategoryId == id);
        if (!details.Success || category == null)
        {
            Fail(details.Message ?? "We could not find that category.");
            return RedirectToAction(nameof(Categories), new { eventId });
        }

        ViewBag.EventTitle = details.Data!.Title;

        return View("CategoryForm", new CategoryFormViewModel
        {
            EventId = eventId,
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            DistanceKm = category.DistanceKm,
            MaxParticipants = category.MaxParticipants
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditCategory(CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return View("CategoryForm", model);

        var result = await Api.PutAsync($"api/categories/{model.CategoryId}", new
        {
            categoryName = model.CategoryName.Trim(),
            distanceKm = model.DistanceKm,
            maxParticipants = model.MaxParticipants
        });

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message ?? "We could not save the category. Please try again.");
            return View("CategoryForm", model);
        }

        Success("The category has been updated.");
        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int eventId, int id)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (result.Success) Success("The category has been deleted.");
        else Fail(result.Message);

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    // -------------------------------------------------------------- Enrolments

    public async Task<IActionResult> Enrolments(int eventId)
    {
        var loaded = await LoadEnrolmentsAsync(eventId);
        if (loaded.Error != null) return loaded.Error;

        return View(loaded.Model);
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int eventId, int enrolmentId, string status)
    {
        var result = await Api.PutAsync($"api/organiser/enrolments/{enrolmentId}/status", new { status });

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (result.Success) Success($"The enrolment is now {status}.");
        else Fail(result.Message);

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // ----------------------------------------------------------------- Results

    public async Task<IActionResult> Results(int eventId)
    {
        var loaded = await LoadEnrolmentsAsync(eventId);
        if (loaded.Error != null) return loaded.Error;

        return View(loaded.Model);
    }

    [HttpPost]
    public async Task<IActionResult> RecordResult(ResultFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            Fail(FirstError() ?? "Please check the finish time and position.");
            return RedirectToAction(nameof(Results), new { eventId = model.EventId });
        }

        // The finish time must be typed as hh:mm:ss (for example 00:25:30).
        if (!TimeSpan.TryParseExact(model.FinishTimeText.Trim(), new[] { @"h\:mm\:ss", @"hh\:mm\:ss" },
                CultureInfo.InvariantCulture, out var finishTime))
        {
            Fail("Enter the finish time as hh:mm:ss, for example 00:25:30.");
            return RedirectToAction(nameof(Results), new { eventId = model.EventId });
        }

        var result = await Api.PostAsync($"api/enrolments/{model.EnrolmentId}/result", new
        {
            finishTime,
            position = model.Position
        });

        var expired = SessionExpired(result);
        if (expired != null) return expired;

        if (result.Success) Success("The result has been saved and published.");
        else Fail(result.Message);

        return RedirectToAction(nameof(Results), new { eventId = model.EventId });
    }

    // ----------------------------------------------------------------- Helpers

    private static object EventBody(EventFormViewModel model) => new
    {
        title = model.Title.Trim(),
        description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
        eventDate = model.EventDate,
        location = model.Location.Trim(),
        distanceKm = model.DistanceKm,
        eventType = model.EventType
    };

    private string? FirstError() =>
        ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault(m => !string.IsNullOrWhiteSpace(m));

    /// <summary>Loads an event and everyone enrolled in it (used by the Enrolments and Results pages).</summary>
    private async Task<(EventEnrolmentsViewModel? Model, IActionResult? Error)> LoadEnrolmentsAsync(int eventId)
    {
        var details = await Api.GetAsync<EventDetails>($"api/events/{eventId}/details");

        var expired = SessionExpired(details);
        if (expired != null) return (null, expired);

        if (!details.Success || details.Data == null)
        {
            Fail(details.Message);
            return (null, RedirectToAction(nameof(Events)));
        }

        var rows = await Api.GetAsync<List<EnrolmentRow>>($"api/organiser/events/{eventId}/enrolments");

        expired = SessionExpired(rows);
        if (expired != null) return (null, expired);

        if (!rows.Success)
        {
            Fail(rows.Message);
            return (null, RedirectToAction(nameof(Events)));
        }

        return (new EventEnrolmentsViewModel { Event = details.Data, Rows = rows.Data ?? new List<EnrolmentRow>() }, null);
    }
}
