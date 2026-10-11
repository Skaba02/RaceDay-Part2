using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Users;

public class UpdateProfileDto
{
    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}
