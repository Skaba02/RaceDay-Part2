using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Auth;
using RaceDay.Api.Helpers;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Registration and login. No login required for these two endpoints.</summary>
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher = new();

    public AuthController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Creates a new user account and assigns a role (Organiser or Participant).</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var normalisedRole = dto.Role.Trim();
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName.ToLower() == normalisedRole.ToLower());

        if (role == null)
        {
            return BadRequest(new { message = "Role must be either 'Organiser' or 'Participant'." });
        }

        var emailExists = await _context.Users.AnyAsync(u => u.Email.ToLower() == dto.Email.ToLower());
        if (emailExists)
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            RoleId = role.RoleId
        };
        // Password is hashed here - it is never stored in its original form.
        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var response = new AuthResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = role.RoleName
        };

        return CreatedAtAction(nameof(Register), new { id = user.UserId }, response);
    }

    /// <summary>Validates credentials, starts a session, and returns the logged-in user's details.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        // Session management: the user's Id and Role are stored server-side
        // and tied to their session cookie for all later requests.
        HttpContext.Session.SetInt32(SessionKeys.UserId, user.UserId);
        HttpContext.Session.SetString(SessionKeys.Role, user.Role!.RoleName);

        return Ok(new AuthResponseDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role.RoleName
        });
    }

    /// <summary>Clears the current session (logs the user out).</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}
