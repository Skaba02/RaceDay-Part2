using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Categories;

public class CategoryCreateDto
{
    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required]
    public decimal DistanceKm { get; set; }

    public int MaxParticipants { get; set; } = 100;
}
