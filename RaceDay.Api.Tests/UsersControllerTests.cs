using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Dtos.Users;
using Xunit;

namespace RaceDay.Api.Tests;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public UsersControllerTests(CustomWebApplicationFactory factory)
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

    [Fact]
    public async Task GetMyProfile_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMyProfile_WhenLoggedIn_ReturnsOwnDetails()
    {
        var client = NewClient();
        var email = $"user{Guid.NewGuid()}@test.com";
        await RegisterAndLoginAsync(client, email, "Participant");

        var response = await client.GetAsync("/api/users/me");
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(email, profile!.Email);
        Assert.Equal("Participant", profile.Role);
    }

    [Fact]
    public async Task UpdateMyProfile_WhenLoggedIn_ChangesFullName()
    {
        var client = NewClient();
        var email = $"user{Guid.NewGuid()}@test.com";
        await RegisterAndLoginAsync(client, email, "Organiser");

        var response = await client.PutAsJsonAsync("/api/users/me", new UpdateProfileDto
        {
            FullName = "Updated Name",
            Email = email
        });
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Updated Name", profile!.FullName);
    }

    [Fact]
    public async Task UpdateMyProfile_WithoutLogin_ReturnsUnauthorized()
    {
        var response = await NewClient().PutAsJsonAsync("/api/users/me", new UpdateProfileDto
        {
            FullName = "No Login",
            Email = "nologin@test.com"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
