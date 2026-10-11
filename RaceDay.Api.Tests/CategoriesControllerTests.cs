using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Dtos.Events;
using Xunit;

namespace RaceDay.Api.Tests;

public class CategoriesControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CategoriesControllerTests(CustomWebApplicationFactory factory)
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

    private static async Task<int> CreateEventAsync(HttpClient organiser)
    {
        var response = await organiser.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            Title = "Category Test Event",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Pretoria",
            EventType = "Run"
        });

        var created = await response.Content.ReadFromJsonAsync<EventResponseDto>();
        return created!.EventId;
    }

    private static CategoryCreateDto ValidCategory() => new()
    {
        CategoryName = "10km Open",
        DistanceKm = 10,
        MaxParticipants = 100
    };

    private static async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId)
    {
        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", ValidCategory());
        var created = await response.Content.ReadFromJsonAsync<CategoryResponseDto>();
        return created!.CategoryId;
    }

    [Fact]
    public async Task GetCategories_ForExistingEvent_WithoutLogin_ReturnsOk()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);

        var publicClient = NewClient();
        var response = await publicClient.GetAsync($"/api/events/{eventId}/categories");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCategories_ForUnknownEvent_ReturnsNotFound()
    {
        var response = await NewClient().GetAsync("/api/events/999999/categories");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsOwningOrganiser_ReturnsCreated()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);

        var response = await organiser.PostAsJsonAsync($"/api/events/{eventId}/categories", ValidCategory());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsDifferentOrganiser_ReturnsForbidden()
    {
        var owner = NewClient();
        await RegisterAndLoginAsync(owner, $"owner{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(owner);

        var otherOrganiser = NewClient();
        await RegisterAndLoginAsync(otherOrganiser, $"other{Guid.NewGuid()}@test.com", "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync($"/api/events/{eventId}/categories", ValidCategory());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_AsParticipant_ReturnsForbidden()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);

        var participant = NewClient();
        await RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");

        var response = await participant.PostAsJsonAsync($"/api/events/{eventId}/categories", ValidCategory());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().PostAsJsonAsync("/api/events/1/categories", ValidCategory());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_AsOwningOrganiser_ReturnsOk()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var response = await organiser.PutAsJsonAsync($"/api/categories/{categoryId}", new CategoryCreateDto
        {
            CategoryName = "21km Open",
            DistanceKm = 21,
            MaxParticipants = 50
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsOwningOrganiser_ReturnsNoContent()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(organiser);
        var categoryId = await CreateCategoryAsync(organiser, eventId);

        var response = await organiser.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_AsDifferentOrganiser_ReturnsForbidden()
    {
        var owner = NewClient();
        await RegisterAndLoginAsync(owner, $"owner{Guid.NewGuid()}@test.com", "Organiser");
        var eventId = await CreateEventAsync(owner);
        var categoryId = await CreateCategoryAsync(owner, eventId);

        var otherOrganiser = NewClient();
        await RegisterAndLoginAsync(otherOrganiser, $"other{Guid.NewGuid()}@test.com", "Organiser");

        var response = await otherOrganiser.DeleteAsync($"/api/categories/{categoryId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
