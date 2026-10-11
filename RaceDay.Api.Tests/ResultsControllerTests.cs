using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Categories;
using RaceDay.Api.Dtos.Enrolments;
using RaceDay.Api.Dtos.Events;
using RaceDay.Api.Dtos.Results;
using Xunit;

namespace RaceDay.Api.Tests;

public class ResultsControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ResultsControllerTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

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

    /// <summary>
    /// Builds the full chain the Results endpoints depend on:
    /// Organiser creates Event + Category, Participant enrols. Returns the ids needed by the tests.
    /// </summary>
    private async Task<(HttpClient Organiser, HttpClient Participant, int CategoryId, int EnrolmentId)> SetUpEnrolmentAsync()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");

        var eventResponse = await organiser.PostAsJsonAsync("/api/events", new EventCreateDto
        {
            Title = "Results Test Event",
            EventDate = DateTime.UtcNow.AddDays(30),
            Location = "Bloemfontein",
            EventType = "Run"
        });
        var ev = await eventResponse.Content.ReadFromJsonAsync<EventResponseDto>();

        var categoryResponse = await organiser.PostAsJsonAsync($"/api/events/{ev!.EventId}/categories", new CategoryCreateDto
        {
            CategoryName = "5km Fun Run",
            DistanceKm = 5,
            MaxParticipants = 100
        });
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryResponseDto>();

        var participant = NewClient();
        await RegisterAndLoginAsync(participant, $"part{Guid.NewGuid()}@test.com", "Participant");

        var enrolResponse = await participant.PostAsync($"/api/categories/{category!.CategoryId}/enrol", null);
        var enrolment = await enrolResponse.Content.ReadFromJsonAsync<EnrolmentResponseDto>();

        return (organiser, participant, category.CategoryId, enrolment!.EnrolmentId);
    }

    private static ResultCreateDto ValidResult() => new()
    {
        FinishTime = TimeSpan.FromMinutes(25),
        Position = 1
    };

    [Fact]
    public async Task CreateResult_AsOwningOrganiser_ReturnsCreated()
    {
        var setup = await SetUpEnrolmentAsync();

        var response = await setup.Organiser.PostAsJsonAsync(
            $"/api/enrolments/{setup.EnrolmentId}/result", ValidResult());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateResult_Twice_ReturnsConflict()
    {
        var setup = await SetUpEnrolmentAsync();
        await setup.Organiser.PostAsJsonAsync($"/api/enrolments/{setup.EnrolmentId}/result", ValidResult());

        var second = await setup.Organiser.PostAsJsonAsync(
            $"/api/enrolments/{setup.EnrolmentId}/result", ValidResult());

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateResult_AsDifferentOrganiser_ReturnsForbidden()
    {
        var setup = await SetUpEnrolmentAsync();

        var otherOrganiser = NewClient();
        await RegisterAndLoginAsync(otherOrganiser, $"other{Guid.NewGuid()}@test.com", "Organiser");

        var response = await otherOrganiser.PostAsJsonAsync(
            $"/api/enrolments/{setup.EnrolmentId}/result", ValidResult());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateResult_AsParticipant_ReturnsForbidden()
    {
        var setup = await SetUpEnrolmentAsync();

        var response = await setup.Participant.PostAsJsonAsync(
            $"/api/enrolments/{setup.EnrolmentId}/result", ValidResult());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateResult_ForUnknownEnrolment_ReturnsNotFound()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");

        var response = await organiser.PostAsJsonAsync("/api/enrolments/999999/result", ValidResult());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateResult_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().PostAsJsonAsync("/api/enrolments/1/result", ValidResult());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCategoryResults_WithoutLogin_ReturnsOk()
    {
        var setup = await SetUpEnrolmentAsync();
        await setup.Organiser.PostAsJsonAsync($"/api/enrolments/{setup.EnrolmentId}/result", ValidResult());

        var response = await NewClient().GetAsync($"/api/categories/{setup.CategoryId}/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetCategoryResults_ForUnknownCategory_ReturnsNotFound()
    {
        var response = await NewClient().GetAsync("/api/categories/999999/results");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsParticipant_ReturnsOk()
    {
        var setup = await SetUpEnrolmentAsync();
        await setup.Organiser.PostAsJsonAsync($"/api/enrolments/{setup.EnrolmentId}/result", ValidResult());

        var response = await setup.Participant.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetMyResults_AsOrganiser_ReturnsForbidden()
    {
        var organiser = NewClient();
        await RegisterAndLoginAsync(organiser, $"org{Guid.NewGuid()}@test.com", "Organiser");

        var response = await organiser.GetAsync("/api/users/me/results");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
