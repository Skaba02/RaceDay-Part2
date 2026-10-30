using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos.Part3;

/// <summary>One event in a list (participant home page and search results).</summary>
public class EventListItemDto
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal? DistanceKm { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string OrganiserName { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
    public bool HasBanner { get; set; }
}

/// <summary>A category shown on the event detail page.</summary>
public class CategoryInfoDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal DistanceKm { get; set; }
    public int MaxParticipants { get; set; }
    public int EnrolledCount { get; set; }
    public bool IsFull => MaxParticipants > 0 && EnrolledCount >= MaxParticipants;
}

/// <summary>Full event information with its categories.</summary>
public class EventDetailsDto : EventListItemDto
{
    public int OrganiserId { get; set; }
    public List<CategoryInfoDto> Categories { get; set; } = new();
}

/// <summary>One event on the Organiser dashboard.</summary>
public class OrganiserEventSummaryDto
{
    public int EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public int CategoryCount { get; set; }
    public int TotalEnrolments { get; set; }
    public bool HasBanner { get; set; }
}

/// <summary>The Organiser dashboard.</summary>
public class OrganiserDashboardDto
{
    public int TotalEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public DateTime? NextEventDate { get; set; }
    public List<OrganiserEventSummaryDto> Events { get; set; } = new();
}

/// <summary>One participant's enrolment, as the Organiser sees it.</summary>
public class EnrolmentRowDto
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string ParticipantEmail { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public bool HasResult { get; set; }
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

/// <summary>Used by the Organiser to change an enrolment status.</summary>
public class EnrolmentStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

/// <summary>One enrolment, as the Participant sees it.</summary>
public class MyEnrolmentDto
{
    public int EnrolmentId { get; set; }
    public int EventId { get; set; }
    public int CategoryId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime EnrolmentDate { get; set; }
    public bool HasResult { get; set; }
}

/// <summary>One line of a Participant's race history.</summary>
public class MyResultDto
{
    public int ResultId { get; set; }
    public int EventId { get; set; }
    public string EventTitle { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
    public int TotalFinishers { get; set; }
}

/// <summary>The logged-in user's profile.</summary>
public class ProfileDto
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool HasProfilePicture { get; set; }
}
