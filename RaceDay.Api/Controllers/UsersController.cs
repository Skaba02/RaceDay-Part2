using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos.Users;
using RaceDay.Api.Filters;
using RaceDay.Api.Helpers;

namespace RaceDay.Api.Controllers;

/// <summary>Profile endpoints for the currently logged-in user, either role.</summary>
[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    [HttpGet("me")]
    [RequireAuth]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return NotFound(new { message = "User not found." });

        return Ok(new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.RoleName
        });
    }

    /// <summary>Updates the profile of the currently logged-in user.</summary>
    [HttpPut("me")]
    [RequireAuth]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile(UpdateProfileDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = HttpContext.Session.GetInt32(SessionKeys.UserId)!.Value;
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return NotFound(new { message = "User not found." });

        user.FullName = dto.FullName.Trim();
        user.Email = dto.Email.Trim();
        await _context.SaveChangesAsync();

        return Ok(new UserProfileDto
        {
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role!.RoleName
        });
    }
}
