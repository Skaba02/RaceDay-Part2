using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Api;

namespace RaceDay.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name can be up to 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a password.")]
    [MinLength(6, ErrorMessage = "Your password needs at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please type your password again.")]
    [Compare(nameof(Password), ErrorMessage = "The two passwords do not match.")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose how you will use RaceDay.")]
    [Display(Name = "I am a")]
    public string Role { get; set; } = "Participant";
}

public class EventFilter
{
    public string? Q { get; set; }
    public string? EventType { get; set; }
    public string? Location { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

public class EventListViewModel
{
    public EventFilter Filter { get; set; } = new();
    public List<EventListItem> Events { get; set; } = new();
}

public class EventDetailsViewModel
{
    public EventDetails Event { get; set; } = new();
    public HashSet<int> EnrolledCategoryIds { get; set; } = new();
}

public class EventFormViewModel
{
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the event name.")]
    [StringLength(150, ErrorMessage = "The name can be up to 150 characters.")]
    [Display(Name = "Event name")]
    public string Title { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "The description can be up to 2000 characters.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Please choose the date and time.")]
    [Display(Name = "Date and time")]
    public DateTime? EventDate { get; set; }

    [Required(ErrorMessage = "Please enter the location.")]
    [StringLength(150, ErrorMessage = "The location can be up to 150 characters.")]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }

    [Required(ErrorMessage = "Please choose the event type.")]
    [Display(Name = "Event type")]
    public string EventType { get; set; } = "Run";

    public bool HasBanner { get; set; }

    [Display(Name = "Event banner image")]
    public IFormFile? Banner { get; set; }
}

public class CategoryFormViewModel
{
    public int EventId { get; set; }
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please enter the category name, for example 10km Open or Under 20.")]
    [StringLength(100, ErrorMessage = "The name can be up to 100 characters.")]
    [Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "The distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Range(1, 100000, ErrorMessage = "Please enter a number of places between 1 and 100000.")]
    [Display(Name = "Maximum participants")]
    public int MaxParticipants { get; set; } = 100;
}

public class CategoriesViewModel
{
    public EventDetails Event { get; set; } = new();
    public CategoryFormViewModel NewCategory { get; set; } = new();
}

public class EventEnrolmentsViewModel
{
    public EventDetails Event { get; set; } = new();
    public List<EnrolmentRow> Rows { get; set; } = new();
}

public class ResultFormViewModel
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }

    [Required(ErrorMessage = "Please enter the finish time as hh:mm:ss, for example 00:25:30.")]
    public string FinishTimeText { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the finishing position.")]
    [Range(1, 1000000, ErrorMessage = "The position must be 1 or more.")]
    public int? Position { get; set; }
}

public class ProfileViewModel
{
    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100, ErrorMessage = "Your name can be up to 100 characters.")]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "That does not look like an email address.")]
    [StringLength(150)]
    public string Email { get; set; } = string.Empty;

    public string? Role { get; set; }
    public bool HasProfilePicture { get; set; }
}
