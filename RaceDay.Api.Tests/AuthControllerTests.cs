using System.Net;
using System.Net.Http.Json;
using RaceDay.Api.Dtos.Auth;
using Xunit;

namespace RaceDay.Api.Tests;

public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        var dto = new RegisterDto
        {
            FullName = "Test Organiser",
            Email = $"organiser{Guid.NewGuid()}@test.com",
            Password = "Password123",
            Role = "Organiser"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRole_ReturnsBadRequest()
    {
        var dto = new RegisterDto
        {
            FullName = "Test User",
            Email = $"baduser{Guid.NewGuid()}@test.com",
            Password = "Password123",
            Role = "Referee"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", dto);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var email = $"dupe{Guid.NewGuid()}@test.com";
        var dto = new RegisterDto
        {
            FullName = "First User",
            Email = email,
            Password = "Password123",
            Role = "Participant"
        };
        await _client.PostAsJsonAsync("/api/auth/register", dto);

        var secondDto = new RegisterDto
        {
            FullName = "Second User",
            Email = email,
            Password = "Password123",
            Role = "Participant"
        };
        var response = await _client.PostAsJsonAsync("/api/auth/register", secondDto);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectCredentials_ReturnsOk()
    {
        var email = $"logintest{Guid.NewGuid()}@test.com";
        var registerDto = new RegisterDto
        {
            FullName = "Login Test",
            Email = email,
            Password = "Password123",
            Role = "Participant"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto { Email = email, Password = "Password123" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var email = $"wrongpass{Guid.NewGuid()}@test.com";
        var registerDto = new RegisterDto
        {
            FullName = "Wrong Pass Test",
            Email = email,
            Password = "Password123",
            Role = "Participant"
        };
        await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new LoginDto { Email = email, Password = "WrongPassword" };
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
