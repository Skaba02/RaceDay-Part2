using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Auth;

public class LoginDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}
