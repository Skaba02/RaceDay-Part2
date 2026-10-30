using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Dtos.Enrolments;
using RaceDay.Api.Dtos.Events;
using RaceDay.Api.Dtos.Part3;
using RaceDay.Api.Dtos.Results;
using Xunit;

namespace RaceDay.Api.Tests;

/// <summary>Tests for the endpoints added for the Part 3 website.</summary>
public class Part3ApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public Part3ApiTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // Each client keeps its own session cookie, so two clients = two different logged-in users.
    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

    private static async Task RegisterAndLoginAsync(HttpClient client, string email, string role)
    {
        await client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Test " + role,
            Email = email,
            Password = "Password123",
            Role = role
        });

        await client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = "Password123"
        });
    }

    private static async Task<int> CreateEventAsync(HttpClient organiser, string title, string eventType = "Run")
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            Title = title,
            Description = "A test event",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Pretoria",
            DistanceKm = 10,
            EventType = eventType
        });

        var created = await response.Content.ReadFromJsonAsync<EventResponseDto>();
        return created!.EventId;
    }

    private static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", new CategoryCreateDto
        {
            CategoryName = "10km Open",
            DistanceKm = 10,
            MaxParticipants = 100
        });

        var created = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        return created!.CategoryId;
    }

    private static async Task<int> EnrolAsync(HttpClient participant, int categoryId)
    {
        var response = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var created = await response.Content.ReadFromJsonAsync<EnrolmentResponseDto>();
        return created!.EnrolmentId;
    }

    [Fact]
    public async Task SearchEvents_WithoutLogin_ReturnsOk()
    {
        var response = await NewClient().GetAsync("/api/events/search");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SearchEvents_FilterByEventType_ReturnsOnlyMatchingEvents()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var token = Guid.NewGuid().ToString("N");
        await CreateEventAsync(organiser, $"Run {token}", "Run");
        await CreateEventAsync(organiser, $"Cycle {token}", "Cycle");

        var results = await NewClient().GetFromJsonAsync<List<EventListItemDto>>(
            $"/api/events/search?q={token}&eventType=Cycle");

        Assert.NotNull(results);
        Assert.Single(results!);
        Assert.Equal("Cycle", results![0].EventType);
    }

    [Fact]
    public async Task EventDetails_ReturnsTheEventWithItsCategories()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser, "Details Event");
        await CreateCategoryAsync(organiser, eventId);

        var details = await NewClient().GetFromJsonAsync<EventDetailsDto>($"/api/events/{eventId}/details");

        Assert.NotNull(details);
        Assert.Equal("Details Event", details!.Title);
        Assert.Single(details.Categories);
    }

    [Fact]
    public async Task EventDetails_ForUnknownEvent_ReturnsNotFound()
    {
        var response = await NewClient().GetAsync("/api/events/999999/details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().GetAsync("/api/organiser/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_AsParticipant_ReturnsForbidden()
    {
        var participant = NewClient();
        await RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");

        var response = await participant.GetAsync("/api/organiser/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_AfterAParticipantEnrols_ShowsOneEnrolment()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser, "Dashboard Event");
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var participant = NewClient();
        await RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");
        await EnrolAsync(participant, categoryId);

        var dashboard = await organiser.GetFromJsonAsync<OrganiserDashboardDto>("/api/organiser/dashboard");

        Assert.NotNull(dashboard);
        Assert.Equal(1, dashboard!.TotalEvents);
        Assert.Equal(1, dashboard.TotalEnrolments);
        Assert.Equal("Dashboard Event", dashboard.Events[0].Title);
    }

    [Fact]
    public async Task MyEnrolments_ShowsTheEventAndStatus()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser, "My Enrolment Event");
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var participant = NewClient();
        await RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");
        await EnrolAsync(participant, categoryId);

        var items = await participant.GetFromJsonAsync<List<MyEnrolmentDto>>("/api/me/enrolments");

        Assert.NotNull(items);
        Assert.Single(items!);
        Assert.Equal("My Enrolment Event", items![0].EventTitle);
        Assert.False(string.IsNullOrEmpty(items[0].Status));
    }

    [Fact]
    public async Task MyResults_AfterTheOrganiserRecordsAResult_ShowsPositionAndTotalFinishers()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser, "Results Event");
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var participant = NewClient();
        await RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");
        var enrolmentId = await EnrolAsync(participant, categoryId);

        await organiser.PostAsJsonAsync($"/api/enrolments/{enrolmentId}/result", new ResultCreateDto
        {
            FinishTime = TimeSpan.FromMinutes(25),
            Position = 1
        });

        var results = await participant.GetFromJsonAsync<List<MyResultDto>>("/api/me/results");

        Assert.NotNull(results);
        Assert.Single(results!);
        Assert.Equal("Results Event", results![0].EventTitle);
        Assert.Equal(1, results[0].Position);
        Assert.Equal(1, results[0].TotalFinishers);
    }

    [Fact]
    public async Task DeleteEvent_WhenItHasEnrolments_ReturnsConflict()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser, "Delete Event");
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var participant = NewClient();
        await RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");
        await EnrolAsync(participant, categoryId);

        var response = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task UploadBanner_WhenStorageIsNotSetUp_ReturnsServiceUnavailable()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser, "Banner Event");

        var file = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        var form = new MultipartFormDataContent { { file, "file", "banner.png" } };

        var response = await organiser.PostAsync($"/api/organiser/events/{eventId}/banner", form);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}
