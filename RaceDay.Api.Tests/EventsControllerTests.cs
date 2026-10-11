using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Events;
using Xunit;

namespace RaceDay.Api.Tests;

public class EventsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public EventsControllerTests(CustomWebApplicationFactory factory)
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
    public async Task GetEvents_WithoutLogin_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsOrganiser_ReturnsCreated()
    {
        await RegisterAndLoginAsync($"org{Guid.NewGuid()}@test.com", "Organiser");

        var dto = new EventCreateDto
        {
            Title = "City Fun Run",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Johannesburg",
            EventType = "Run"
        };

        var response = await _client.PostAsJsonAsync("/api/events", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_ReturnsForbidden()
    {
        await RegisterAndLoginAsync($"part{Guid.NewGuid()}@test.com", "Participant");

        var dto = new EventCreateDto
        {
            Title = "Should Not Work",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Cape Town",
            EventType = "Run"
        };

        var response = await _client.PostAsJsonAsync("/api/events", dto);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutLogin_ReturnsUnauthorized()
    {
        var dto = new EventCreateDto
        {
            Title = "No Login Event",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Durban",
            EventType = "Run"
        };

        var response = await _client.PostAsJsonAsync("/api/events", dto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
