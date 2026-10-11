using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Dtos.Events;
using Xunit;

namespace RaceDay.Api.Tests;

public class EnrolmentsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EnrolmentsControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
    }

    private async Task RegisterAndLoginAsync(string email, string role)
    {
        await _client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            FullName = "Test " + role,
            Email = email,
            Password = "Password123",
            Role = role
        });

        await _client.PostAsJsonAsync("/api/auth/login", new LoginDto
        {
            Email = email,
            Password = "Password123"
        });
    }

    [Fact]
    public async Task Enrol_AsParticipant_ReturnsCreated()
    {
        await RegisterAndLoginAsync($"org{Guid.NewGuid()}@test.com", "Organiser");

        var eventResponse = await _client.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            Title = "Enrolment Test Event",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Pretoria",
            EventType = "Run"
        });
        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventResponseDto>();

        var categoryResponse = await _client.PostAsJsonAsync(
            $"/api/events/{createdEvent!.EventId}/categories",
            new CategoryCreateDto { CategoryName = "10km", DistanceKm = 10, MaxParticipants = 50 });
        var createdCategory = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        // Logging in again as a Participant overwrites the session for this client's cookie.
        await RegisterAndLoginAsync($"part{Guid.NewGuid()}@test.com", "Participant");

        var enrolResponse = await _client.PostAsync($"/api/categories/{createdCategory!.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, enrolResponse.StatusCode);
    }

    [Fact]
    public async Task Enrol_Twice_ReturnsConflict()
    {
        await RegisterAndLoginAsync($"org{Guid.NewGuid()}@test.com", "Organiser");

        var eventResponse = await _client.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            Title = "Duplicate Enrolment Test",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Soweto",
            EventType = "Run"
        });
        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventResponseDto>();

        var categoryResponse = await _client.PostAsJsonAsync(
            $"/api/events/{createdEvent!.EventId}/categories",
            new CategoryCreateDto { CategoryName = "5km", DistanceKm = 5, MaxParticipants = 50 });
        var createdCategory = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        await RegisterAndLoginAsync($"part{Guid.NewGuid()}@test.com", "Participant");

        await _client.PostAsync($"/api/categories/{createdCategory!.CategoryId}/enrol", null);
        var secondAttempt = await _client.PostAsync($"/api/categories/{createdCategory.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_ReturnsForbidden()
    {
        await RegisterAndLoginAsync($"org{Guid.NewGuid()}@test.com", "Organiser");

        var eventResponse = await _client.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            Title = "Organiser Enrol Test",
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Polokwane",
            EventType = "Run"
        });
        var createdEvent = await eventResponse.Content.ReadFromJsonAsync<EventResponseDto>();

        var categoryResponse = await _client.PostAsJsonAsync(
            $"/api/events/{createdEvent!.EventId}/categories",
            new CategoryCreateDto { CategoryName = "21km", DistanceKm = 21, MaxParticipants = 50 });
        var createdCategory = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        // Still logged in as the Organiser from above - Organisers cannot enrol.
        var enrolResponse = await _client.PostAsync($"/api/categories/{createdCategory!.CategoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, enrolResponse.StatusCode);
    }
}
